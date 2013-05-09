using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.CollectionQuery.Test.Domain;
using NHibernate.Dialect;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;

namespace NHibernate.CollectionQuery.Test
{
    [TestFixture]
    public abstract class QueryableCollectionFixture
    {
        Configuration configuration;
        ISessionFactory sessionFactory;
        object id;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            configuration = new Configuration();
            configuration.SessionFactory()
                         .Integrate.Using<SQLiteDialect>()
                         .Connected.Using("Data source=testdb")
                         .AutoQuoteKeywords();
            var mapper = new ConventionModelMapper();
            mapper.Class<Foo>(cm => { });
            mapper.Class<Bar>(cm => { });
            CustomizeMapping(mapper);
            var mappingDocument = mapper.CompileMappingForAllExplicitlyAddedEntities();
            new XmlSerializer(typeof(HbmMapping)).Serialize(Console.Out, mappingDocument);
            configuration.AddDeserializedMapping(mappingDocument, "Mappings");
            new SchemaExport(configuration).Create(true, true);
            sessionFactory = configuration.BuildSessionFactory();
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                var foo = new Foo { Bars = CreateCollection() };
                foo.Bars.Add(new Bar { Data = 1 });
                foo.Bars.Add(new Bar { Data = 2 });
                id = session.Save(foo);
                tx.Commit();
            }
            sessionFactory.Statistics.IsStatisticsEnabled = true;
        }

        protected abstract ICollection<Bar> CreateCollection();

        protected abstract void CustomizeMapping(ConventionModelMapper mapper);

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            new SchemaExport(configuration).Drop(false, true);
        }

        [SetUp]
        public void Setup()
        {
            sessionFactory.Statistics.Clear();
        }

        void QueryWithoutInitializing(Action<ICollection<Bar>, Bar> verify)
        {
            using (var session = sessionFactory.OpenSession())
            {
                var foo = session.Get<Foo>(id);
                var bar = foo.Bars.AsQueryable().Single(b => b.Data == 2);
                verify(foo.Bars, bar);
            }
        }

        void QueryInitialized(Action<ICollection<Bar>, Bar> verify)
        {
            using (var session = sessionFactory.OpenSession())
            {
                var foo = session.Get<Foo>(id);
                NHibernateUtil.Initialize(foo.Bars);
                var bar = foo.Bars.AsQueryable().Single(b => b.Data == 2);
                verify(foo.Bars, bar);
            }
        }

        [Test]
        public void LinqQueriesDontCauseInitialization()
        {
            QueryWithoutInitializing((bars, bar) =>
                                     Assert.False(NHibernateUtil.IsInitialized(bars), "collection was initialized"));
        }

        [Test]
        public void LinqQueriesDontLoadAdditionalEntities()
        {
            QueryWithoutInitializing((bars, bar) =>
                                     Assert.AreEqual(2, sessionFactory.Statistics.EntityLoadCount,
                                                     "unexpected numer of entities loaded"));
        }

        [Test]
        public void LinqQueriesOnInitializedCollectionsReturnTheRightElement()
        {
            QueryInitialized((bars, bar) =>
                             {
                                 Assert.NotNull(bar, "could not retrieve collection element");
                                 Assert.AreEqual(2, bar.Data, "invalid element retrieved");
                             });
        }

        [Test]
        public void LinqQueriesOnUninitializedCollectionsReturnTheRightElement()
        {
            QueryWithoutInitializing((bars, bar) =>
                                     {
                                         Assert.NotNull(bar, "could not retrieve collection element");
                                         Assert.AreEqual(2, bar.Data, "invalid element retrieved");
                                     });
        }

        [Test]
        public void AlreadyInitializedCollectionsAreQueriedInMemory()
        {
            QueryInitialized((bars, bar) =>
                             Assert.AreEqual(0, sessionFactory.Statistics.QueryExecutionCount,
                                             "unexpected query execution"));
        }
    }
}