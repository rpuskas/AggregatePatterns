using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Dapper;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;

namespace AggregatePatterns
{

    // Dapper: 19280 to 48620 = 29MB @ .88 seconds
    // NHibernate: 59472 to 270316 = 210MB @ 5 seconds

    [TestFixture]
    public class MyTestFixture
    {
        private static ISessionFactory CreateSessionFactory()
        {
            return Fluently.Configure()
                .Database(MsSqlConfiguration.MsSql2008.ConnectionString(
                @"Server=TW-WINDOWS\SQLEXPRESS;Database=AggregatePatterns;Trusted_Connection=True;"))
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<MyTestFixture>())
                .BuildSessionFactory();
        }

        [Test]
        public void LoadTheData()
        {
            var sessionFactory = CreateSessionFactory();
            using (var session = sessionFactory.OpenSession())
            {
                for (var i = 0; i < 100000; i++)
                {
                    var t = new Trade { Amount = i };
                    var c = new Clearance { Amount = i };
                    var m = new Match(t, c);
                    session.SaveOrUpdate(m);
                }
                
                session.Flush();
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
                    .Fetch(x => x.Trade).ToList();

                //var serializedResult = JsonConvert.SerializeObject(result);
                //Console.WriteLine("TotalSize: {0} MB", GetSize(serializedResult));

                stopwatch.Stop();
                Console.WriteLine("Elapsed: {0}", stopwatch.ElapsedMilliseconds);
            }
            
        }

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
    public class DapperFixture
    {
        [Test]
        public void ShouldDoSometingInteresting()
        {
            
            using (IDbConnection conn = new SqlConnection(@"Server=TW-WINDOWS\SQLEXPRESS;Database=AggregatePatterns;Trusted_Connection=True;"))
            {
                var stopwatch = Stopwatch.StartNew();
    
                var result = conn.Query<Match, Trade, Clearance, Match>(
                    "SELECT * FROM MATCH " +
                    "INNER JOIN Trade ON Trade.Id = Match.Trade_Id " +
                    "INNER JOIN Clearance ON Clearance.Id = Match.Trade_Id",
                    (match, trade, clearance) =>
                    {
                        match.Trade = trade;
                        match.Clearance = clearance;
                        return match;
                    });

                //var serializedResult = JsonConvert.SerializeObject(result);
                //Console.WriteLine("TotalSize: {0} MB", GetSize(serializedResult));

                stopwatch.Stop();
                Console.WriteLine("Elapsed: {0}", stopwatch.ElapsedMilliseconds);
            }
        }

        public decimal GetSize(object obj)
        {
            using (Stream s = new MemoryStream())
            {
                new BinaryFormatter().Serialize(s, obj);
                return decimal.Round(s.Length / 1024.0m / 1024, 3);
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
        public virtual int Id { get; set; }
        public virtual decimal Amount { get; set; }
    }

    public class Clearance
    {
        public virtual int Id { get; set; }
        public virtual decimal Amount { get; set; }
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
