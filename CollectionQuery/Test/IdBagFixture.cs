using System.Collections.Generic;
using NHibernate.CollectionQuery.Test.Domain;
using NHibernate.Mapping.ByCode;

namespace NHibernate.CollectionQuery.Test
{
    public class IdBagFixture : QueryableCollectionFixture
    {
        protected override ICollection<Bar> CreateCollection()
        {
            return new List<Bar>();
        }

        protected override void CustomizeMapping(ConventionModelMapper mapper)
        {
            mapper.Class<Foo>(cm => cm.IdBag(x => x.Bars,
                                             bpm =>
                                             {
                                                 bpm.Cascade(Cascade.All);
                                                 bpm.Type<PersistentQueryableIdBagType<Bar>>();
                                             },
                                             cer => cer.ManyToMany()));
        }
 
    }
}