namespace AggregatePatterns
{
    public abstract class EntityBase<T> where T : EntityBase<T>
    {
        public virtual int Id { get; set; }

        protected bool IsTransient { get { return Id == 0; } }

        public override bool Equals(object obj)
        {
            return EntityEquals(obj as EntityBase<T>);
        }

        protected bool EntityEquals(EntityBase<T> other)
        {
            if (other == null)
            {
                return false;
            }
            if (IsTransient ^ other.IsTransient)
            {
                return false;
            }
            if (IsTransient && other.IsTransient)
            {
                return ReferenceEquals(this, other);
            }

            return Id == other.Id;
        }

       
        public override int GetHashCode()
        {
            return IsTransient ? base.GetHashCode() : Id.GetHashCode();
        }

        public static bool operator ==(EntityBase<T> x, EntityBase<T> y)
        {
            return Equals(x, y);
        }

        public static bool operator !=(EntityBase<T> x, EntityBase<T> y)
        {
            return !(x == y);
        }
    }
}