using FluentNHibernate.Mapping;

namespace AggregatePatterns
{
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
                .Cascade.All();
        }
    }

    public class ClearanceMap : ClassMap<Clearance>
    {
        public ClearanceMap()
        {
            Id(x => x.Id);
            Map(x => x.Amount);
            HasMany(x => x.Adjustments)
                .Cascade.All();
        }
    }

}