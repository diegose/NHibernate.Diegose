using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Linq;
using NHibernate.Proxy;

namespace NHibernate.CollectionQuery
{
    public static class CollectionQueryableExtensions
    {
        public static IQueryable<T> Query<T>(this ICollection<T> source, ISessionImplementor session = null)
        {
            var persistentCollection = source as IPersistentCollection;
            if (persistentCollection == null || persistentCollection.WasInitialized)
                return source.AsQueryable();
            if (session == null)
                session = (ISessionImplementor) typeof (AbstractPersistentCollection)
                                                    .GetProperty("Session",
                                                                 BindingFlags.Instance | BindingFlags.NonPublic)
                                                    .GetValue(persistentCollection, null);
            var queryMethod = typeof(LinqExtensionMethods)
                .GetMethod("Query",
                           new[]
                               {
                                   session is ISession
                                       ? typeof (ISession)
                                       : typeof (IStatelessSession)
                               });
            var ownerProxy = persistentCollection.Owner as INHibernateProxy;
            var ownerType = ownerProxy == null
                                ? persistentCollection.Owner.GetType()
                                : ownerProxy.HibernateLazyInitializer.PersistentClass;
            var ownerParameter = Expression.Parameter(ownerType);
            var collectionPropertyName = persistentCollection.Role.Split('.').Last();
            var selectMany = typeof(Queryable).GetMethods()
                                               .First(x => x.Name == "SelectMany")
                                               .MakeGenericMethod(ownerType, typeof(T));
            dynamic predicate = Expression.Lambda(Expression.Equal(ownerParameter,
                                                                   Expression.Constant(persistentCollection.Owner,
                                                                                       ownerType)),
                                                  ownerParameter);
            var collectionSelector = Expression.Lambda(typeof(Func<,>)
                                                               .MakeGenericType(ownerType,
                                                                                typeof(IEnumerable<>)
                                                                                    .MakeGenericType(typeof(T))),
                                                           Expression.Property(ownerParameter, collectionPropertyName),
                                                           ownerParameter);
            dynamic ownerQueryable = queryMethod.MakeGenericMethod(ownerType).Invoke(null, new object[] { session });
            var ownerQuery = Queryable.Where(ownerQueryable, predicate);
            var elementsQuery = selectMany.Invoke(null, new object[] { ownerQuery, collectionSelector });
            return (IQueryable<T>)elementsQuery;
        }
    }
}