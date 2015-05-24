using FluentNHibernate.Mapping;

namespace AggregatePatterns
{
    public class Match
    {
        public int Id { get; set; }
        
        public Trade Trade { get; set; }
        
        public Clearance Clearance { get; set; }

        public Match(Trade trade, Clearance clearance)
        {
            Trade = trade;
            Clearance = clearance;
        }
    }

    public class Trade
    {
        public int Id { get; set; }
        public decimal Amount;
    }

    public class Clearance
    {
        public int Id { get; set; }
        public decimal Amount;
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
