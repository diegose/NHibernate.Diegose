#region License

//
//  MemCache - A cache provider for NHibernate using the .NET client
//  (http://sourceforge.net/projects/memcacheddotnet) for memcached,
//  which is located at http://www.danga.com/memcached/.
//
//  This library is free software; you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation; either
//  version 2.1 of the License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// CLOVER:OFF
//

#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using log4net.Config;
using NHibernate.Cache;
using NUnit.Framework;

namespace NHibernate.Caches.MemCache.Tests
{
	[TestFixture]
	public class MemCacheFixture
	{
		private MemCacheProvider provider;
		private Dictionary<string, string> props;

		[OneTimeSetUp]
		public void FixtureSetup()
		{
			XmlConfigurator.Configure();
			props = new Dictionary<string, string> {{"compression_enabled", "false"}, {"expiration", "20"}};
			provider = new MemCacheProvider();
			provider.Start(props);
		}

		[OneTimeSetUp]
		public void FixtureStop()
		{
            if (provider != null)
            {
                provider.Stop();
            }
		}

		[Test]
		public void TestClear()
		{
			string key = "key1";
			string value = "value";

			ICache cache = provider.BuildCache("nunit", props);
			Assert.IsNotNull(cache, "no cache returned");

			// add the item
			cache.Put(key, value);
			Thread.Sleep(1000);

			// make sure it's there
			object item = cache.Get(key);
			Assert.IsNotNull(item, "couldn't find item in cache");

			// clear the cache
			cache.Clear();

			// make sure we don't get an item
			item = cache.Get(key);
			Assert.IsNull(item, "item still exists in cache");
		}

		[Test]
		public void TestDefaultConstructor()
		{
			ICache cache = new MemCacheClient();
			Assert.IsNotNull(cache);
		}

		[Test]
		public void TestEmptyProperties()
		{
			ICache cache = new MemCacheClient("nunit", new Dictionary<string, string>());
			Assert.IsNotNull(cache);
		}

		[Test]
		public void TestNoPropertiesConstructor()
		{
			ICache cache = new MemCacheClient("nunit");
			Assert.IsNotNull(cache);
		}

		[Test]
		public void TestNullKeyGet()
		{
			ICache cache = new MemCacheClient();
			cache.Put("nunit", "value");
			Thread.Sleep(1000);
			object item = cache.Get(null);
			Assert.IsNull(item);
		}

		[Test]
		public void TestNullKeyPut()
		{
			ICache cache = new MemCacheClient();
			Assert.Throws<ArgumentNullException>(() => cache.Put(null, null));
		}

		[Test]
		public void TestNullKeyRemove()
		{
			ICache cache = new MemCacheClient();
			Assert.Throws<ArgumentNullException>(() => cache.Remove(null));
		}

		[Test]
		public void TestNullValuePut()
		{
			ICache cache = new MemCacheClient();
			Assert.Throws<ArgumentNullException>(() => cache.Put("nunit", null));
		}

		[Test]
		public void TestPut()
		{
			string key = "key1";
			string value = "value";

			ICache cache = provider.BuildCache("nunit", props);
			Assert.IsNotNull(cache, "no cache returned");

			Assert.IsNull(cache.Get(key), "cache returned an item we didn't add !?!");

			cache.Put(key, value);
			Thread.Sleep(1000);
			object item = cache.Get(key);
			Assert.IsNotNull(item);
			Assert.AreEqual(value, item, "didn't return the item we added");
		}

		[Test]
		public void TestRegions()
		{
			string key = "key";
			ICache cache1 = provider.BuildCache("nunit1", props);
			ICache cache2 = provider.BuildCache("nunit2", props);
			string s1 = "test1";
			string s2 = "test2";
			cache1.Put(key, s1);
			cache2.Put(key, s2);
			Thread.Sleep(1000);
			object get1 = cache1.Get(key);
			object get2 = cache2.Get(key);
			Assert.IsFalse(get1 == get2);
		}

		[Test]
		public void TestRemove()
		{
			string key = "key1";
			string value = "value";

			ICache cache = provider.BuildCache("nunit", props);
			Assert.IsNotNull(cache, "no cache returned");

			// add the item
			cache.Put(key, value);
			Thread.Sleep(1000);

			// make sure it's there
			object item = cache.Get(key);
			Assert.IsNotNull(item, "item just added is not there");

			// remove it
			cache.Remove(key);

			// make sure it's not there
			item = cache.Get(key);
			Assert.IsNull(item, "item still exists in cache");
		}

        [Test]
        public void TestRemove144()
        {
            string key = "key1";
            string value = "value";

            //memcached 1.4+ drops support for expiration time specified for Delete operations
            //therefore if you install memcached 1.4.4 this test will fail unless corresponding fix is implemented in MemCacheClient.cs
            //the test will fail because Remove won't actually delete the item from the cache!
            //the error you will see in the log is: "Error deleting key: nunit@key1.  Server response: CLIENT_ERROR bad command line format.  Usage: delete <key> [noreply]"

            //Now, Memcached.ClientLibrary incorrectly divides expiration time for Delete operation by 1000
            //(for Add and Set operations the expiration time is calculated correctly)
            //that's why we need to set expiration to 20000, otherwise it will be treated as 20ms which is too small to be sent to server (the minimum value is 1 second)
            props["expiration"] = "20000";

            //disabling lingering delete will cause the item to get immediately deleted
            //this parameter is NEW and the code to make it work is part of the proposed fix
            props.Add("lingering_delete_disabled", "true"); 

            ICache cache = provider.BuildCache("nunit", props);
            Assert.IsNotNull(cache, "no cache returned");

            // add the item
            cache.Put(key, value);
            Thread.Sleep(1000);

            // make sure it's there
            object item = cache.Get(key);
            Assert.IsNotNull(item, "item just added is not there");

            // remove it
            cache.Remove(key);

            // make sure it's not there
            item = cache.Get(key);
            Assert.IsNull(item, "item still exists in cache");
        }

	}
}