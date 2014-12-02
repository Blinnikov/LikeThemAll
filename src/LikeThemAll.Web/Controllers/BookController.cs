using Microsoft.AspNet.Mvc;
using MongoDB.Driver;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace LikeThemAll.Controllers
{
    public class BookController : Controller
    {
        // GET: /<controller>/
        public IActionResult Index()
        {
            var connectionString = "mongodb://localhost:27017";
            var client = new MongoClient(connectionString);
            var server = client.GetServer();
            var db = server.GetDatabase("test");
            var collectionName = db.GetCollection("Igor-Blinnikov").FullName;
            return View("Index", collectionName);
        }
    }
}
