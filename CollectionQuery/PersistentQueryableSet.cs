using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate.Collection.Generic;
using Iesi.Collections.Generic;
using NHibernate.Engine;

namespace NHibernate.CollectionQuery
{
    public class PersistentQueryableSet<T> : PersistentGenericSet<T>, IQueryable<T>
    {
        IQueryable queryable;

        public PersistentQueryableSet()
        {
        }

        public PersistentQueryableSet(ISessionImplementor sessionImplementor)
            : base(sessionImplementor)
        {
        }

        public PersistentQueryableSet(ISessionImplementor sessionImplementor, ICollection<T> original)
            : base(sessionImplementor, original as Iesi.Collections.Generic.ISet<T> ?? new HashedSet<T>(original))
        {
        }

        public Expression Expression
        {
            get { return GetQueryable().Expression; }
        }

        public System.Type ElementType
        {
            get { return typeof(T); }
        }

        public IQueryProvider Provider
        {
            get { return GetQueryable().Provider; }
        }

        IQueryable GetQueryable()
        {
            return queryable ?? (queryable = WasInitialized ? gset.AsQueryable() : this.Query(Session));
        }
    }
}