using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate.Collection.Generic;
using NHibernate.Engine;

namespace NHibernate.CollectionQuery
{
    public class PersistentQueryableList<T> : PersistentGenericList<T>, IQueryable<T>
    {
        IQueryable queryable;

        public PersistentQueryableList()
        {
        }

        public PersistentQueryableList(ISessionImplementor sessionImplementor)
            : base(sessionImplementor)
        {
        }

        public PersistentQueryableList(ISessionImplementor sessionImplementor, IList<T> collection)
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
            return queryable ?? (queryable = WasInitialized ? glist.AsQueryable() : this.Query(Session));
        }
    }
}