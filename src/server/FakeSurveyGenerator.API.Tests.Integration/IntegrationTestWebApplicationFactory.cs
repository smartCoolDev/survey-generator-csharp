﻿using System;
using System.Linq;
using FakeSurveyGenerator.API.Identity;
using FakeSurveyGenerator.Application.Common.Identity;
using FakeSurveyGenerator.Data;
using FakeSurveyGenerator.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FakeSurveyGenerator.API.Tests.Integration
{
    public class IntegrationTestWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__SurveyContext", "Server=sqlserver;Database=FakeSurveyGenerator;user id=SA;pwd=<YourStrong!Passw0rd>;ConnectRetryCount=0");
            Environment.SetEnvironmentVariable("IDENTITY_PROVIDER_URL", "https://test.com");

            builder.ConfigureServices(services =>
            {
                RemoveDefaultDbContextFromServiceCollection(services);
                RemoveDefaultDistributedCacheFromServiceCollection(services);

                services.AddDistributedMemoryCache();

                services.AddDbContext<SurveyContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });

                services.AddScoped<IUser>(provider => new ApiUser("test-id", "Test User", "test.user@test.com"));

                var rootServiceProvider = services.BuildServiceProvider();

                using var scope = rootServiceProvider.CreateScope();

                var scopedServiceProvider = scope.ServiceProvider;
                var context = scopedServiceProvider.GetRequiredService<SurveyContext>();
                var logger = scopedServiceProvider
                    .GetRequiredService<ILogger<IntegrationTestWebApplicationFactory<TStartup>>>();

                var cache = scopedServiceProvider.GetRequiredService<IDistributedCache>();

                cache.Remove("FakeSurveyGenerator");

                context.Database.EnsureCreated();

                try
                {
                    DatabaseSeed.SeedSampleData(context);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred seeding the database with test surveys. Error: {Message}", ex.Message);
                }
            });
        }

        private static void RemoveDefaultDbContextFromServiceCollection(IServiceCollection services)
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<SurveyContext>));
            if (descriptor == null) return;
            services.Remove(descriptor);
        }

        private static void RemoveDefaultDistributedCacheFromServiceCollection(IServiceCollection services)
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDistributedCache));
            if (descriptor == null) return;
            services.Remove(descriptor);
        }
    }
}
