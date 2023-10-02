namespace JwtDemo.Dtos
{
    public class ProductCreateDto
    {
        public string Title { get; set; } = string.Empty;

        public required IFormFile Image { get; set; }
    }
}
