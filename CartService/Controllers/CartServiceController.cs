using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CartService.Controllers
{
    using System.Data;
    using DAL;
    using DAL.Models;
    using Dapper;
    using Microsoft.Data.SqlClient;
    using Microsoft.Extensions.Configuration;

    [ApiController]
    [Route("[controller]")]
    public class CartServiceController : ControllerBase
    {
        private readonly string connectionString;
        private const string CART_UID_FIELD = "CART_UID_FIELD";

        public CartServiceController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private static Dictionary<Guid, List<Product>> _products=new Dictionary<Guid, List<Product>>();
        private Guid _cartUid;

        [HttpGet]
        public ActionResult<IEnumerable<Product>> Get()
        {
            Product[] products = null;
            
            using(IDbConnection db = new SqlConnection(connectionString))
            {
                products = db.Query<Product>(Resources.CartService_GetProducts, new {uid = CartUid}).ToArray();
            }
            
            return products;
        }

        [HttpPost()]
        public IActionResult Update(ProductInCartDTO[] products)
        {
            if (products == null)
            {
                throw new ArgumentNullException("Список продуктов не может принимать значение null", nameof(products));
            }

            using (IDbConnection db = new SqlConnection(connectionString))
            {
                db.Open();
                using (var transaction = db.BeginTransaction())
                {
                    db.Execute(Resources.CartService_DeleteProducts, new {uid = CartUid}, transaction);

                    if (products.Length > 0)
                    {
                        var shoppingCartId = db.ExecuteScalar<int>(
                            Resources.CartService_GetOrCreateCartId,
                            new {uid = CartUid},
                            transaction);

                        var shoppingCartProductsIds =
                            products
                                .Where(product => product.Amount > 0)
                                .Select(product => new
                                {
                                    product_id = product.Id,
                                    shoppingCart_id = shoppingCartId,
                                    amount = product.Amount
                                });

                        db.Execute(
                            Resources.CartService_UpdateProducts,
                            shoppingCartProductsIds, 
                            transaction);
                    }
                    else
                    {
                        db.Execute(
                            Resources.CartService_DeleteCart,
                            new {uid = CartUid},
                            transaction);
                    }
                    
                    transaction.Commit();
                }
            }

            return this.Ok();
        }

        private Guid CartUid
        {
            get
            {
                var requestCartUid = Request.Cookies[CART_UID_FIELD];
                if (_cartUid != Guid.Empty)
                    return _cartUid;

                if (requestCartUid == null
                    || !Guid.TryParse(requestCartUid, out _cartUid))
                {
                    _cartUid = Guid.NewGuid();
                    Response.Cookies.Append(CART_UID_FIELD, _cartUid.ToString());
                }

                return _cartUid;
            }
        }
    }
}