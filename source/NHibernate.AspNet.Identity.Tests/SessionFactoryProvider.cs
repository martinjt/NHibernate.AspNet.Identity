﻿using System;
using System.Linq;
using System.IO;
using NHibernate.AspNet.Identity.DomainModel;
using NHibernate.Cfg;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;

namespace NHibernate.AspNet.Identity.Tests
{
    public sealed class SessionFactoryProvider
    {
        private static volatile SessionFactoryProvider _instance;
        private static object _syncRoot = new Object();

        private Configuration _configuration;

        public ISessionFactory SessionFactory { get; private set; }
        public string Name { get; private set; }

        /// <summary>
        /// constructor configures a SessionFactory based on the configuration passed in
        /// </summary>
        private SessionFactoryProvider()
        {
            Name = "NHibernate.AspNet.Identity";

            var baseEntityToIgnore = new[] { 
                typeof(EntityWithTypedId<int>), 
                typeof(EntityWithTypedId<string>), 
            };

            var allEntities = new[] { 
                typeof(IdentityUser), 
                typeof(IdentityRole), 
                typeof(IdentityUserLogin), 
                typeof(IdentityUserClaim), 
            };

            var mapper = new ConventionModelMapper();
            //DefineBaseClass(mapper, baseEntityToIgnore);
            //mapper.IsComponent((type, declared) => typeof(ValueObject).IsAssignableFrom(type));

            mapper.AddMapping<IdentityUserMap>();
            mapper.AddMapping<IdentityRoleMap>();
            mapper.AddMapping<IdentityUserClaimMap>();

            var mapping = mapper.CompileMappingForAllExplicitlyAddedEntities();
            //var mapping = mapper.CompileMappingFor(allEntities);
            Console.WriteLine(mapping.AsString());

            _configuration = new Configuration();
            _configuration.Configure("sqlite-nhibernate-config.xml");
            _configuration.AddDeserializedMapping(mapping, null);


            //log4net.Config.XmlConfigurator.Configure();
            SessionFactory = _configuration.BuildSessionFactory();
        }

        public static SessionFactoryProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncRoot)
                    {
                        if (_instance == null)
                            _instance = new SessionFactoryProvider();
                    }
                }
                return _instance;
            }
        }

        public void BuildSchema()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"schema.sql");

            // this NHibernate tool takes a configuration (with mapping info in)
            // and exports a database schema from it
            new SchemaExport(_configuration)
                .SetOutputFile(path)
                .Create(true, true /* DROP AND CREATE SCHEMA */);
        }

        private static void DefineBaseClass(ConventionModelMapper mapper, System.Type[] baseEntityToIgnore)
        {
            if (baseEntityToIgnore == null) return;
            mapper.IsEntity((type, declared) =>
                baseEntityToIgnore.Any(x => x.IsAssignableFrom(type)) &&
                !baseEntityToIgnore.Any(x => x == type) &&
                !type.IsInterface);
            mapper.IsRootEntity((type, declared) => baseEntityToIgnore.Any(x => x == type.BaseType));
        }

    }
}
