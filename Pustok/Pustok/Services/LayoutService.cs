using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Pustok.DAL;
using Pustok.Models;
using Pustok.ViewModels;

namespace Pustok.Services
{
    public class LayoutService
    {
        private readonly PustokDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LayoutService(PustokDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public List<Genre> GetGenres()
        {
            return _context.Genres.ToList();
        }

        public Dictionary<string,string> GetSettings()
        {
            return _context.Settings.ToDictionary(x => x.Key, x => x.Value);
        }

        public BasketViewModel GetBasket()
        {
            BasketViewModel basketVM = new BasketViewModel();

            List<BasketCookieItemViewModel> basketItems = new List<BasketCookieItemViewModel>();

            var basketJson = _httpContextAccessor.HttpContext.Request.Cookies["basket"];
           
            if (basketJson != null)
                basketItems = JsonConvert.DeserializeObject<List<BasketCookieItemViewModel>>(basketJson);


            foreach (var item in basketItems)
            {

                var book = _context.Books.Include(x=>x.BookImages.Where(bi=>bi.PosterStatus==true)).FirstOrDefault(x => x.Id == item.BookId);

                basketVM.BasketItems.Add(new BasketItemViewModel
                {
                    Book = book,
                    Count = item.Count
                });

                var price = book.DiscountPercent > 0?(book.SalePrice*(100-book.DiscountPercent)/100) : book.SalePrice;
                basketVM.TotalPrice += (price * item.Count);
            }


            return basketVM;
        }
    }
}
