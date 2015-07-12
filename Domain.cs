using System;
using System.Collections.Generic;

namespace AggregatePatterns
{
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
            Adjustments = new HashSet<Adjustment>();
        }

        public virtual int Id { get; set; }
        public virtual decimal Amount { get; set; }
        public virtual ISet<Adjustment> Adjustments { get; set; }
    }

    public class Adjustment : EntityBase<Adjustment>
    {
        public virtual decimal Amount { get; set; }
    }

    public class Clearance
    {
        public Clearance()
        {
            Adjustments = new HashSet<Adjustment>();
        }

        public virtual int Id { get; set; }
        public virtual decimal Amount { get; set; }
        public virtual ISet<Adjustment> Adjustments { get; set; }
    }
}
