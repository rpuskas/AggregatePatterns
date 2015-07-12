using System;
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
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Linq;
using NHibernate.SqlCommand;
using NHibernate.Transform;
using NHibernate.Util;
using NUnit.Framework;

namespace AggregatePatterns
{
    public class BaseFixture
    {
        protected readonly static string ConnectionString =
            @"Server=TW-WINDOWS\SQLEXPRESS;Database=AggregatePatterns;Trusted_Connection=True;";
        
        public const int TradeCount = 100000;
        public const int TradeAdjustmentCount = 2;
        public const int ClearingAdjustmentCount = 3;

        public decimal GetSize(object obj)
        {
            using (Stream s = new MemoryStream())
            {
                new BinaryFormatter().Serialize(s, obj);
                return decimal.Round(s.Length / 1024.0m / 1024, 3);
            }
        }

        protected static void OutputElapsedTime(Stopwatch stopwatch)
        {
            Console.WriteLine("Elapsed: {0}", stopwatch.ElapsedMilliseconds);
        }

        protected void OutputSerializedSize(IList<Match> myResult)
        {
            var serializedResult = JsonConvert.SerializeObject(myResult);
            Console.WriteLine("TotalSize: {0} MB", GetSize(serializedResult));
        }

        protected static void VerifyResults(IList<Match> myResult)
        {
            Assert.That(myResult.Count, Is.EqualTo(TradeCount));
            myResult.ForEach(x => Assert.That(x.Trade.Adjustments.Count, Is.EqualTo(TradeAdjustmentCount)));
            myResult.ForEach(x => Assert.That(x.Clearance.Adjustments.Count, Is.EqualTo(ClearingAdjustmentCount)));    
            var actual =
                myResult.SelectMany(x => x.Trade.Adjustments).Union(
                    myResult.SelectMany(x => x.Clearance.Adjustments));
            Assert.That(actual.Count(), Is.EqualTo(TradeCount*(ClearingAdjustmentCount + TradeAdjustmentCount)));
        }
    }

    [TestFixture]
    public class MyTestFixture : BaseFixture
    {
        private static ISessionFactory CreateSessionFactory()
        {
            var conn = MsSqlConfiguration.MsSql2008.ConnectionString(
                ConnectionString);

            //conn.ShowSql();
            //conn.FormatSql();

            return Fluently.Configure()
                .Database(conn)
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<MyTestFixture>())
                .BuildSessionFactory();
        }

        //[Test]
        public void LoadData()
        {
            var sessionFactory = CreateSessionFactory();
            using (var session = sessionFactory.OpenSession())
            {
                var stopwatch = Stopwatch.StartNew();
                for (var i = 0; i < TradeCount; i++)
                {
                    var t = new Trade { Amount = i };
                    for (var j = 0; j < TradeAdjustmentCount; j++)
                    {
                        t.Adjustments.Add(new Adjustment { Amount = 11 });
                    }

                    var c = new Clearance { Amount = i };
                    for (var j = 0; j < ClearingAdjustmentCount; j++)
                    {
                        c.Adjustments.Add(new Adjustment { Amount = 22 });
                    }

                    var m = new Match(t, c);
                    session.Save(m);
                }
                session.Flush();
                OutputElapsedTime(stopwatch);
            }
        }

        [Test]
        public void ShouldSerializeResults_Query()
        {
            SerializeResults((s) =>
            {
                return s.Query<Match>()
                    .Fetch(x => x.Trade)
                    .ThenFetchMany(x => x.Adjustments)
                    .Fetch(x => x.Clearance)
                    .ThenFetchMany(x => x.Adjustments).ToList();
            });
        }   

        [Test]
        public void ShouldSerializeResults_QueryOver()
        {
            SerializeResults((session) =>
            {
                Match match = null;
                Trade trade = null;
                Clearance clearance = null;
                Adjustment clearingAdjustment = null, tradingAdjustment = null;

                return session.QueryOver(() => match)
                    .JoinAlias(() => match.Trade, () => trade)
                    .JoinAlias(() => match.Clearance, () => clearance)
                    .JoinAlias(() => match.Clearance.Adjustments, () => clearingAdjustment, JoinType.LeftOuterJoin)
                    .JoinAlias(() => trade.Adjustments, () => tradingAdjustment, JoinType.LeftOuterJoin)
                    .TransformUsing(Transformers.DistinctRootEntity).List();
            });
        }

        private void SerializeResults(Func<ISession, IList<Match>> action)
        {
            IList<Match> result;
            var sessionFactory = CreateSessionFactory();
            var stopwatch = Stopwatch.StartNew();
            using (var session = sessionFactory.OpenSession())
            {
                result = action(session);
            }

            OutputSerializedSize(result);
            OutputElapsedTime(stopwatch);
            VerifyResults(result);
        }
    }


    [TestFixture]
    public class DapperFixture : BaseFixture
    {
        [Test]
        public void ShouldHashAdjustmentsOnIdentifier()
        {
            var hashSet = new HashSet<Adjustment> {new Adjustment {Id = 1}, new Adjustment {Id = 1}};
            Assert.That(hashSet.Count, Is.EqualTo(1));
        }

        [Test]
        public void ShouldSerializeResults()
        {
            using (IDbConnection connection = new SqlConnection(ConnectionString))
            {
                var stopwatch = Stopwatch.StartNew();
                var myResult = new List<Match>();
                var result = connection.Query<Match, Trade, Clearance, Adjustment, Adjustment, Match>(
                    "SELECT * FROM MATCH " +
                    "INNER JOIN Trade ON Trade.Id = Match.Trade_Id " +
                    "INNER JOIN Clearance ON Clearance.Id = Match.Trade_Id " +
                    "LEFT OUTER JOIN Adjustment TA ON Trade.Id = TA.Trade_Id " +
                    "LEFT OUTER JOIN Adjustment CA ON Clearance.Id = CA.Clearance_Id",
                    (match, trade, clearance, tradeAdjustment, clearanceAdjustment) =>
                    {
                        match.Trade = trade;
                        match.Clearance = clearance;
                        match.Trade.Adjustments.Add(tradeAdjustment);
                        match.Clearance.Adjustments.Add(clearanceAdjustment);
                        return match;
                    });

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
                        last.Clearance.Adjustments.Add(x.Clearance.Adjustments.Single());
                    }
                });

                OutputSerializedSize(myResult);
                OutputElapsedTime(stopwatch);

                VerifyResults(myResult);
            }
        }
    }
}