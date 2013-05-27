using System;
using System.Linq;
using System.Xml.Serialization;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Dialect;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;
using NodaTime;

namespace NHibernate.NodaTime.Test
{
    public class LocalDateFixture
    {
        ISessionFactory sessionFactory;
        static readonly LocalDate someDate = new LocalDate(1979, 7, 17);

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var configuration = new Configuration();
            configuration.SessionFactory()
                         .Integrate.Using<SQLiteDialect>()
                         .Connected.Using("Data source=testdb")
                         .AutoQuoteKeywords()
                         .LogSqlInConsole()
                         .EnableLogFormattedSql();
            var mapper = new ConventionModelMapper();
            MapClasses(mapper);
            var mappingDocument = mapper.CompileMappingForAllExplicitlyAddedEntities();
            new XmlSerializer(typeof(HbmMapping)).Serialize(Console.Out, mappingDocument);
            configuration.AddDeserializedMapping(mappingDocument, "Mappings");
            new SchemaExport(configuration).Create(true, true);
            sessionFactory = configuration.BuildSessionFactory();
        }

        [SetUp]
        public void Setup()
        {
            using (var session = sessionFactory.OpenSession())
                session.CreateQuery("delete Foo").ExecuteUpdate();
        }

        [Test]
        public void ValuesCanBePersistedAndRetrieved()
        {
            object id;
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                id = session.Save(new Foo { Date = someDate });
                tx.Commit();
            }
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                var foo = session.Get<Foo>(id);
                Assert.AreEqual(someDate, foo.Date);
            }
        }

        [Test]
        public void ValuesCanBeUsedInQueries()
        {
            object id;
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                id = session.Save(new Foo { Date = someDate });
                tx.Commit();
            }
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                var foo = session.Query<Foo>().Single(x=>x.Date == someDate);
                Assert.AreEqual(id, session.GetIdentifier(foo));
            }
        }

        [Test]
        public void ValuesCanBeRetrievedFromQueries()
        {
            object id;
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                id = session.Save(new Foo { Date = someDate });
                tx.Commit();
            }
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                var maxDate = session.CreateQuery("select max(Date) from Foo").UniqueResult<LocalDate>();
                Assert.AreEqual(someDate, maxDate);
            }
        }

        static void MapClasses(ConventionModelMapper mapper)
        {
            mapper.Class<Foo>(cm => cm.Property(x => x.Date, pm => pm.Type<LocalDateType>()));
        }
    }
}