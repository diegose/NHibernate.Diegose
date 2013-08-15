using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Proxy;

namespace NHibernate.CollectionQuery
{
    using Tuple = System.Tuple;
    using Type = System.Type;
    
    public static class CollectionQueryableExtensions
    {
        private delegate object SessionQueryableFunc(object session);
        private delegate object SelectManyFunc(object ownerQueryable, object collectionSelection);

        private static Func<IPersistentCollection, ISessionImplementor> sessionGetter;
        private static ConcurrentDictionary<Tuple<Type, Type>, SessionQueryableFunc> sessionQueryableGetters;
        private static ConcurrentDictionary<Tuple<Type, Type>, SelectManyFunc> selectManyGetters;

        static CollectionQueryableExtensions()
        {
            sessionGetter = CreateSessionGetter();
            sessionQueryableGetters = new ConcurrentDictionary<Tuple<Type, Type>, SessionQueryableFunc>();
            selectManyGetters = new ConcurrentDictionary<Tuple<Type, Type>, SelectManyFunc>();
        }

        private static Func<IPersistentCollection, ISessionImplementor> CreateSessionGetter()
        {           
            var sessionProperty = typeof(AbstractPersistentCollection)
                .GetProperty("Session", BindingFlags.Instance | BindingFlags.NonPublic);

            var collectionParameter = Expression.Parameter(typeof(IPersistentCollection));
   
            var body = Expression.Property(
                Expression.Convert(collectionParameter, typeof(AbstractPersistentCollection)),
                sessionProperty
            );

            return Expression.Lambda<Func<IPersistentCollection, ISessionImplementor>>(body, collectionParameter)
                .Compile();
        }

        private static SessionQueryableFunc CreateSessionQueryableGetter(Tuple<Type, Type> types)
        {
            var sessionType = types.Item1;
            var ownerType = types.Item2;

            var queryMethod = typeof(NHibernate.Linq.LinqExtensionMethods)
                .GetMethod("Query", new[] { sessionType })
                .MakeGenericMethod(ownerType);

            var sessionParameter = Expression.Parameter(typeof(object));

            var body = Expression.Call(null, queryMethod, 
                Expression.Convert(sessionParameter, sessionType)
            );

            return Expression.Lambda<SessionQueryableFunc>(body, sessionParameter)
                .Compile();
        }

        private static SelectManyFunc CreateSelectManyGetter(Tuple<Type, Type> types)
        {
            var ownerType = types.Item1;
            var itemType = types.Item2;

            var selectManyMethod = typeof(Queryable).GetMethods()
                .First(m =>
                {
                    var parameters = m.GetParameters();
                    if (m.Name != "SelectMany" || parameters.Length != 2) return false;

                    var p1 = parameters[1].ParameterType;

                    return p1.GetGenericTypeDefinition() == typeof(Expression<>)
                        && p1.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(Func<,>);
                })
                .MakeGenericMethod(ownerType, itemType);

            var ownerQueryableParameter = Expression.Parameter(typeof(object));
            var collectionSelectorParameter = Expression.Parameter(typeof(object));

            // Build the type "Expression<Func<TOwner, IEnumerable<TItem>>"
            var selectorType = typeof(Expression<>).MakeGenericType(
                GetCollectionSelectorType(ownerType, itemType)
            );

            var body = Expression.Call(null, selectManyMethod,
                Expression.Convert(ownerQueryableParameter, typeof(IQueryable<>).MakeGenericType(ownerType)),
                Expression.Convert(collectionSelectorParameter, selectorType)
            );

            return Expression.Lambda<SelectManyFunc>(body, ownerQueryableParameter, collectionSelectorParameter)
                .Compile();
        }

        public static IQueryable<T> Query<T>(this ICollection<T> source, ISessionImplementor session = null)
        {
            var persistentCollection = source as IPersistentCollection;
            if (persistentCollection == null || persistentCollection.WasInitialized)
                return source.AsQueryable();

            if (session == null)
                session = sessionGetter(persistentCollection);

            var ownerProxy = persistentCollection.Owner as INHibernateProxy;
            var ownerType = ownerProxy == null
                                ? persistentCollection.Owner.GetType()
                                : ownerProxy.HibernateLazyInitializer.PersistentClass;
            var ownerParameter = Expression.Parameter(ownerType);
            var collectionPropertyName = persistentCollection.Role.Split('.').Last();
            dynamic predicate = Expression.Lambda(Expression.Equal(ownerParameter,
                                                                   Expression.Constant(persistentCollection.Owner,
                                                                                       ownerType)),
                                                  ownerParameter);
            var collectionSelector = Expression.Lambda(GetCollectionSelectorType(ownerType, typeof(T)),
                                                           Expression.Property(ownerParameter, collectionPropertyName),
                                                           ownerParameter);
            var sessionType = session is ISession ? typeof(ISession) : typeof(IStatelessSession);
            var queryableGetter = sessionQueryableGetters.GetOrAdd(Tuple.Create(sessionType, ownerType), CreateSessionQueryableGetter);
            dynamic ownerQueryable = queryableGetter(session);
            var ownerQuery = Queryable.Where(ownerQueryable, predicate);

            var selectMany = selectManyGetters.GetOrAdd(Tuple.Create(ownerType, typeof(T)), CreateSelectManyGetter);
            var elementsQuery = selectMany(ownerQuery, collectionSelector);
            return (IQueryable<T>)elementsQuery;
        }

        private static Type GetCollectionSelectorType(Type ownerType, Type itemType)
        {
            // Build the type "Func<TOwner, IEnumerable<TItem>"
            return typeof(Func<,>)
                .MakeGenericType(ownerType,
                    typeof(IEnumerable<>).MakeGenericType(itemType)
                );
        }
    }
}