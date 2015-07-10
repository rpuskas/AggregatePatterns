using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Dapper;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Conventions;
using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Util;
using NUnit.Framework;

namespace AggregatePatterns
{

    // Dapper: 19280 to 48620 = 29MB @ .88 seconds
    // NHibernate: 59472 to 270316 = 210MB @ 5 seconds

    public class BaseFixture
    {
        public decimal GetSize(object obj)
        {
            using (Stream s = new MemoryStream())
            {
                new BinaryFormatter().Serialize(s, obj);
                return decimal.Round(s.Length / 1024.0m / 1024, 3);
            }
        }
    }

    [TestFixture]
    public class MyTestFixture : BaseFixture
    {
        private static ISessionFactory CreateSessionFactory()
        {
            var conn = MsSqlConfiguration.MsSql2008.ConnectionString(
                @"Server=TW-WINDOWS\SQLEXPRESS;Database=AggregatePatterns;Trusted_Connection=True;");
           
            //conn.ShowSql();
            //conn.FormatSql();

            return Fluently.Configure()
                .Database(conn)
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<MyTestFixture>())
                .BuildSessionFactory();
        }

        [Test]
        public void LoadTheData()
        {
            var sessionFactory = CreateSessionFactory();
            using (var session = sessionFactory.OpenSession())
            {
                var stopwatch = Stopwatch.StartNew();
                for (var i = 0; i < 100000; i++)
                {
                    var t = new Trade { Amount = i };
                    for (var j = 0; j < 2; j++) { t.Adjustments.Add(new Adjustment { Amount = 10 }); }
                    var c = new Clearance { Amount = i };
                    var m = new Match(t, c);
                    session.Save(m);
                }
                session.Flush();
                Console.WriteLine("Elapsed: {0}", stopwatch.ElapsedMilliseconds);
            }
        }

        [Test]
        public void SerializeTheResults()
        {
            var sessionFactory = CreateSessionFactory();
            using (var session = sessionFactory.OpenSession())
            {
                var stopwatch = Stopwatch.StartNew();

                var result = session.Query<Match>()
                    .Fetch(x => x.Clearance)
                    .Fetch(x => x.Trade)
                    .ThenFetchMany(x => x.Adjustments).ToList();

                var serializedResult = JsonConvert.SerializeObject(result);
                Console.WriteLine("TotalSize: {0} MB", GetSize(serializedResult));

                stopwatch.Stop();
                Console.WriteLine("Elapsed: {0}", stopwatch.ElapsedMilliseconds);
            }
            
        }
    }

    [TestFixture]
    public class DapperFixture : BaseFixture
    {
        [Test]
        public void ShouldDoSometingInteresting()
        {
            using (IDbConnection conn = new SqlConnection(@"Server=TW-WINDOWS\SQLEXPRESS;Database=AggregatePatterns;Trusted_Connection=True;"))
            {
                var stopwatch = Stopwatch.StartNew();
                const int count = 100000;

                var myResult = new List<Match>();
                var result = conn.Query<Match, Trade, Clearance, Adjustment, Match>(
                    "SELECT * FROM MATCH " +
                    "LEFT OUTER JOIN Trade ON Trade.Id = Match.Trade_Id " +
                    "LEFT OUTER JOIN Clearance ON Clearance.Id = Match.Trade_Id " +
                    "LEFT OUTER JOIN Adjustment ON Trade.Id = Adjustment.Trade_Id ",
                    (match, trade, clearance, adjustment) =>
                    {
                        match.Trade = trade;
                        match.Clearance = clearance;
                        match.Trade.Adjustments.Add(adjustment);    
                        return match;
                    });

                //Reduce by Match.Trade
                Match last = null;
                result.OrderBy(x => x.Id).ForEach(x =>
                {
                    if (last == null || last.Id != x.Id)
                    {
                        myResult.Add(x);
                        last = x;
                    }
                    else
                    {
                        last.Trade.Adjustments.Add(x.Trade.Adjustments.Single());
                    }
                });

                var serializedResult = JsonConvert.SerializeObject(myResult);
                Console.WriteLine("TotalSize: {0} MB", GetSize(serializedResult));

                stopwatch.Stop();
                Console.WriteLine("Elapsed: {0}", stopwatch.ElapsedMilliseconds);

                Assert.That(myResult.Count, Is.EqualTo(count));
                myResult.ForEach(x => Assert.That(x.Trade.Adjustments.Count,Is.EqualTo(2)));
                Assert.That(myResult.SelectMany(x => x.Trade.Adjustments).Count(), Is.EqualTo(count * 2));
            }
        }
    }

    public class Match
    {
        public virtual int Id { get; set; }
        
        public virtual Trade Trade { get; set; }
        
        public virtual Clearance Clearance { get; set; }

        protected Match() { }

        public Match(Trade trade, Clearance clearance)
        {
            Trade = trade;
            Clearance = clearance;
        }
    }

    public class Trade
    {
        public Trade()
        {
            Adjustments = new List<Adjustment>();
        }

        public virtual int Id { get; set; }
        public virtual decimal Amount { get; set; }
        public virtual IList<Adjustment> Adjustments { get; set; }
    }

    public class Adjustment
    {
        public virtual int Id { get; set; }
        public virtual decimal Amount { get; set; }
    }

    public class Clearance
    {
        public virtual int Id { get; set; }
        public virtual decimal Amount { get; set; }
    }

    public class AdjustmentMap : ClassMap<Adjustment>
    {
        public AdjustmentMap()
        {
            Id(x => x.Id);
            Map(x => x.Amount);
        }
    }

    public class MatchMap : ClassMap<Match>
    {
        public MatchMap()
        {
            Id(x => x.Id);
            References(x => x.Trade).Cascade.SaveUpdate();
            References(x => x.Clearance).Cascade.SaveUpdate();
        }
    }

    public class TradeMap : ClassMap<Trade>
    {
        public TradeMap()
        {
            Id(x => x.Id);
            Map(x => x.Amount);
            HasMany(x => x.Adjustments)
                .Not.KeyNullable()
                .Cascade.All();
        }
    }

    public class ClearanceMap : ClassMap<Clearance>
    {
        public ClearanceMap()
        {
            Id(x => x.Id);
            Map(x => x.Amount);
        }
    }

}
