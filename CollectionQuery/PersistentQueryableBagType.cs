using System.Collections;
using System.Collections.Generic;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Persister.Collection;
using NHibernate.UserTypes;

namespace NHibernate.CollectionQuery
{
    public class PersistentQueryableBagType<T> : IUserCollectionType
    {
        public IPersistentCollection Instantiate(ISessionImplementor session, ICollectionPersister persister)
        {
            return new PersistentQueryableBag<T>(session);
        }

        public IPersistentCollection Wrap(ISessionImplementor session, object collection)
        {
            return new PersistentQueryableBag<T>(session, (ICollection<T>) collection);
        }

        public IEnumerable GetElements(object collection)
        {
            return (IEnumerable) collection;
        }

        public bool Contains(object collection, object entity)
        {
            return ((ICollection<T>) collection).Contains((T) entity);
        }

        public object IndexOf(object collection, object entity)
        {
            return -1;
        }

        public object ReplaceElements(object original, object target, ICollectionPersister persister, object owner,
                                      IDictionary copyCache, ISessionImplementor session)
        {
            var result = (ICollection<T>)target;
            result.Clear();
            foreach (var item in ((IEnumerable) original))
                if (copyCache.Contains(item))
                    result.Add((T) copyCache[item]);
                else
                    result.Add((T) item);
            return result;
        }

        public object Instantiate(int anticipatedSize)
        {
            return new List<T>();
        }
    }
}