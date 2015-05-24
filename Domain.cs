using System.Linq;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;

namespace AggregatePatterns
{
    [TestFixture]
    public class MyTestFixture
    {
        private static Configuration _configuration;

        private static ISessionFactory CreateSessionFactory()
        {
            return Fluently.Configure()
                .Database(SQLiteConfiguration.Standard.InMemory)
                .ExposeConfiguration(c => _configuration = c)
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<MyTestFixture>())
                .BuildSessionFactory();
        }

        [Test]
        public void Test()
        {
            var sessionFactory = CreateSessionFactory();
            var schemaExport = new SchemaExport(_configuration);
            using (var session = sessionFactory.OpenSession())
            {
                schemaExport.Execute(true, true, false, session.Connection, null);
                var customer = new Trade { Amount = 123 };
                session.SaveOrUpdate(customer);

                session.Flush();

                var foo = session.Query<Trade>().ToList();
                Assert.AreEqual(foo.First().Amount, 123);
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
            References(x => x.Trade);
            References(x => x.Clearance);
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
