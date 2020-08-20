using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CartService
{
    using System.Data;
    using System.IO;
    using System.Net;
    using System.Net.Mail;
    using System.Reflection;
    using System.Xml.Serialization;
    using Coravel;
    using DAL;
    using DAL.Models;
    using Dapper;
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;

    public class Startup
    {
        private string _connectionString;
        private ILogger<Startup> _logger;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<CartServiceDbContext>(opt =>
                opt.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly("CartService.DAL")));
            
            services.AddControllers();
            services.AddScheduler();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration configuration, ILogger<Startup> logger)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

            var expirationDate  = DateTime.Today.AddDays(-30);

            app.ApplicationServices.UseScheduler(scheduler =>
            {
                scheduler.Schedule(() => RemoveExpiredCarts(expirationDate))
                    .DailyAt(18,30);
                scheduler.Schedule(() => SendShoppingCartDailyReport(expirationDate))
                    .EveryMinute();
                //.DailyAt(19,0);
            });
        }

        private void RemoveExpiredCarts(DateTime expirationDate)
        {
            try
            {
                var toRemoveCarts = Resources.ShoppingCart_GetExpirationCartIds;

                int[] removeCartIds = null;
                using (IDbConnection db = new SqlConnection(_connectionString))
                {
                    removeCartIds = db.Query<int>(toRemoveCarts, new {expirationDate})
                        .ToArray();
                }

                string removeOldCarts = string.Format(
                    Resources.ShoppingCart_RemoveOldCarts,
                    Resources.ShoppingCart_GetExpirationCartIds);

                using (IDbConnection db = new SqlConnection(_connectionString))
                {
                    db.Execute(removeOldCarts, new {expirationDate});
                }

                foreach (var removeCartId in removeCartIds)
                {
                    CallHooks(removeCartId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("SendShoppingCartDailyReport ended with error :{0}",e);
            }
        }

        private void SendShoppingCartDailyReport(DateTime expirationDate)
        {
            DailyReport report = null;
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                report=db.Query<DailyReport>(Resources.CartDailyReport, new {expirationDate}).FirstOrDefault();
            }

            if (report == null)
            {
                report = new DailyReport();
            }
            
            try
            {
                
                var path = System.IO.Path.Combine(
                    "Reports",
                    string.Format("ShoppingCartReport{0:yyyy_MM_dd}.txt", DateTime.Today));
                
                using (var filestream = new FileStream(path, FileMode.OpenOrCreate))
                {
                    using (var writer = new StreamWriter(filestream))
                    {
                        var properties = report.GetType()
                            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            .ToDictionary(prop => prop.Name, prop => prop.GetValue(report, null));

                        foreach (var property in properties)
                        {
                            writer.WriteLine("{0}:{1}", property.Key, property.Value);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError("SendShoppingCartDailyReport ended with error :{0}",e);
            }
        }

        private class DailyReport
        {
            public int Count { get; set; }
            public int countWithForBonusProducts { get; set; }
            public int expired10 { get; set; }
            public int expired20 { get; set; }
            public int expired30 { get; set; }
        }

        private void CallHooks(int removeCartId)
        {
            //TODO: сделать таблицу (id, ShoppingCart_id, WebHookUrl)
            //извлекать url-ы по id корзины и вызывать через webRequest 
            //количество попыток вызова
            //В данный момент корзина удаляется вне зависимости от успешности вызоыва веб-хука. 
            //можно помечать каждый веб-хук успешно ли вызван и удалять либо только для успешных, либо все вместе после успешной обработки
            
            // throw new NotImplementedException();
        }
    }
}