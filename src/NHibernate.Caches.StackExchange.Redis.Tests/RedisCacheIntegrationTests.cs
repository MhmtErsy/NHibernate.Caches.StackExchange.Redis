using System;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using System.IO;
using Xunit;
using NHibernate;
using NHibernate.Driver;
using NHibernate.Dialect;
using System.Data.SQLite;


namespace NHibernate.Caches.StackExchange.Redis.Tests
{
    public class RedisCacheIntegrationTests : RedisTest
    {
        private static Configuration configuration;

        public RedisCacheIntegrationTests()
        {
            RedisCacheProvider.ConnectionSettings = ConnectionSettings;

            if (File.Exists("tests.db")) { File.Delete("tests.db"); }

            if (configuration == null)
            {
				configuration = new Configuration();
				configuration.DataBaseIntegration(x =>
					{
                        x.ConnectionString = "Data Source=tests.db;Version=3;New=True:";
						x.Driver<SQLite20Driver>();
                        x.Dialect<SQLiteDialect>();
					});
                configuration.AddAssembly(this.GetType().Assembly);
                configuration.Cache(x =>
                {
                    x.UseQueryCache = true;
                    x.Provider<RedisCacheProvider>();
                    x.DefaultExpiration = 100000;
                });
                configuration.EntityCache<Person>(x =>
                {
                    x.Strategy = EntityCacheUsage.ReadWrite;
                });

                configuration.SetProperty(NHibernate.Cfg.Environment.GenerateStatistics, "true");
                configuration.SetProperty(NHibernate.Cfg.Environment.UseSecondLevelCache, "true");
                configuration.SetProperty(NHibernate.Cfg.Environment.UseQueryCache, "true");
                
                var schema = new SchemaExport(configuration);
                schema.Create(true, true);
            }
        }

        [Fact]
        public void Entity_cache()
        {
            using (var sf = CreateSessionFactory())
            {
                object personId = null;
                
                UsingSession(sf, session =>
                {
                    personId = session.Save(new Person("Foo", 1));
                });

                sf.Statistics.Clear();

                UsingSession(sf, session =>
                {
                    session.Get<Person>(personId);
                }, session => {
                    Assert.Equal("miss: 1, put: 1", string.Format("miss: {0}, put: {1}",
                        sf.Statistics.SecondLevelCacheMissCount, sf.Statistics.SecondLevelCachePutCount));
                });

                sf.Statistics.Clear();

                UsingSession(sf, session =>
                {
                    session.Get<Person>(personId);
                },session=>{
                    Assert.Equal("hit: 1, miss: 0, put: 0", string.Format("hit: {0}, miss: {1}, put: {2}",
                        sf.Statistics.SecondLevelCacheHitCount, sf.Statistics.SecondLevelCacheMissCount, sf.Statistics.SecondLevelCachePutCount));
                });
            }
        }

        private ISessionFactory CreateSessionFactory()
        {
            return configuration.BuildSessionFactory();
        }

        private void UsingSession(ISessionFactory sessionFactory, Action<ISession> action, Action<ISession> asserts=null)
        {
            using (var session = sessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                action(session);
                transaction.Commit();
                if (asserts!=null) asserts(session);
            }
        }
    }
}
