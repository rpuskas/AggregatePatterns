namespace AggregatePatterns
{
    public class Match
    {
        public Trade Trade;

        public Clearance Clearance;

        public Match(Trade trade, Clearance clearance)
        {
            
        }
    }

    public class Trade
    {
        public int Id;
    }

    public class Clearance
    {
        public int Id;
    }

}
