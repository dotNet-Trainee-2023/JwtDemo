using JwtDemo.Data;
using JwtDemo.Dtos;
using JwtDemo.Models;
using Microsoft.AspNetCore.Mvc;

namespace JwtDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _imageProductDirectory;

        public ProductsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
            _imageProductDirectory = env.WebRootPath + @"\Images\Products";
        }

        [HttpGet]
        public ActionResult<IEnumerable<Product>> Get()
        {
            var products = _context.Products.ToList();
            return Ok(products);
        }

        [HttpGet("DeleteImage/{id}")]
        public ActionResult DeleteImage(Guid id)
        {
            var product = _context.Products.Find(id);

            System.IO.File.Delete(_env.WebRootPath + product.ImagePath);

            return Ok();
        }

        [HttpPost]
        public async Task<ActionResult> CreateAsync([FromForm] ProductCreateDto dto)
        {
            if (!Directory.Exists(_imageProductDirectory))
            {
                Directory.CreateDirectory(_imageProductDirectory);
            }

            FileInfo _fileInfo = new FileInfo(dto.Image.FileName);
            string filename = _fileInfo.Name.Replace(_fileInfo.Extension, "") + "_" + DateTime.Now.Ticks.ToString() + _fileInfo.Extension;
            var _filePath = $"{_imageProductDirectory}\\{filename}";
            using (var _fileStream = new FileStream(_filePath, FileMode.Create))
            {
                await dto.Image.CopyToAsync(_fileStream);
            }
            string _urlPath = _filePath.Replace('\\', '/').Split("wwwroot").Last();
            string _imagePath = _filePath.Split("wwwroot").Last();

            Product p = new Product
            {
                Title = dto.Title,
                ImageUrl = _urlPath,
                ImagePath = _imagePath
            };

            _context.Products.Add(p);
            _context.SaveChanges();

            return Ok(p);
        }
    }
}
