using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate.Collection.Generic;
using NHibernate.Engine;

namespace NHibernate.CollectionQuery
{
    public class PersistentQueryableIdBag<T> : PersistentIdentifierBag<T>, IQueryable<T>
    {
        IQueryable queryable;

        public PersistentQueryableIdBag()
        {
        }

        public PersistentQueryableIdBag(ISessionImplementor sessionImplementor)
            : base(sessionImplementor)
        {
        }

        public PersistentQueryableIdBag(ISessionImplementor sessionImplementor, ICollection<T> collection)
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
            return queryable ?? (queryable = WasInitialized ? InternalValues.AsQueryable() : this.Query(Session));
        }
    }
}