using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate.Collection.Generic;
using NHibernate.Engine;

namespace NHibernate.CollectionQuery
{
    public class PersistentQueryableBag<T> : PersistentGenericBag<T>, IQueryable<T>
    {
        IQueryable queryable;

        public PersistentQueryableBag()
        {
        }

        public PersistentQueryableBag(ISessionImplementor sessionImplementor)
            : base(sessionImplementor)
        {
        }

        public PersistentQueryableBag(ISessionImplementor sessionImplementor, ICollection<T> collection)
            : base(sessionImplementor, collection)
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
            return queryable ?? (queryable = WasInitialized ? InternalBag.AsQueryable() : this.Query(Session));
        }
    }
}