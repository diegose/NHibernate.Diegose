using System.Collections.Generic;
using NHibernate.CollectionQuery.Test.Domain;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace NHibernate.CollectionQuery.Test
{
    [TestFixture]
    public class BagFixture : QueryableCollectionFixture
    {
        protected override ICollection<Bar> CreateCollection()
        {
            return new List<Bar>();
        }

        protected override void CustomizeMapping(ConventionModelMapper mapper)
        {
            mapper.Class<Foo>(cm => cm.Bag(x => x.Bars, bpm =>
                                                            {
                                                                bpm.Cascade(Cascade.All);
                                                                bpm.Type<PersistentQueryableBagType<Bar>>();
                                                            }));
        }
    }
}