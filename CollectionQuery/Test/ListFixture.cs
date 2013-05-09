using System.Collections.Generic;
using NHibernate.CollectionQuery.Test.Domain;
using NHibernate.Mapping.ByCode;

namespace NHibernate.CollectionQuery.Test
{
    public class ListFixture : QueryableCollectionFixture
    {
        protected override ICollection<Bar> CreateCollection()
        {
            return new List<Bar>();
        }

        protected override void CustomizeMapping(ConventionModelMapper mapper)
        {
            mapper.Class<Foo>(cm => cm.List(x => x.Bars, bpm =>
                                                         {
                                                             bpm.Cascade(Cascade.All);
                                                             bpm.Type<PersistentQueryableListType<Bar>>();
                                                         }));
        }
    }
}