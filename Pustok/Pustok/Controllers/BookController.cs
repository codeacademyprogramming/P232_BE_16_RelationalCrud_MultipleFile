using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Pustok.DAL;
using Pustok.Models;
using Pustok.ViewModels;
using System.Net;

namespace Pustok.Controllers
{
    public class BookController : Controller
    {
        private readonly PustokDbContext _context;

        public BookController(PustokDbContext context)
        {
            _context = context;
        }

        public IActionResult Detail(int id)
        {
            Book book = _context.Books
                .Include(x=>x.BookImages)
                .Include(x=>x.BookTags).ThenInclude(bt=>bt.Tag)
                .Include(x=>x.Author)
                .Include(x => x.Genre)
                .FirstOrDefault(x=>x.Id == id);

            return View(book);
        }

        public IActionResult GetBookModal(int id)
        {
            var book = _context.Books
                .Include(x=>x.Genre)
                .Include(x => x.Author)
                .Include(x => x.BookImages)
                .FirstOrDefault(x=>x.Id == id);

            return PartialView("_BookModalPartial",book);
        }

        public IActionResult AddToBasket(int id)
        {
            if (_context.Books.Find(id) == null)
            {
                return NotFound();
            }

                List<BasketCookieItemViewModel> basketItems;
            var basket = HttpContext.Request.Cookies["basket"];

            if (basket == null)
                basketItems = new List<BasketCookieItemViewModel>();
            else
                basketItems = JsonConvert.DeserializeObject<List<BasketCookieItemViewModel>>(basket);

            var wantedBook = basketItems.FirstOrDefault(x => x.BookId == id);
            
            if (wantedBook == null)
                basketItems.Add(new BasketCookieItemViewModel { Count = 1, BookId = id });
            else
                wantedBook.Count++;
            HttpContext.Response.Cookies.Append("basket", JsonConvert.SerializeObject(basketItems));

            BasketViewModel basketVM = new BasketViewModel();
            foreach (var item in basketItems)
            {

                var book = _context.Books.Include(x => x.BookImages.Where(x => x.PosterStatus == true)).FirstOrDefault(x => x.Id == item.BookId);

                basketVM.BasketItems.Add(new BasketItemViewModel
                {
                    Book = book,
                    Count = item.Count
                });

                var price = book.DiscountPercent > 0 ? (book.SalePrice * (100 - book.DiscountPercent) / 100) : book.SalePrice;
                basketVM.TotalPrice += (price * item.Count);
            }

            return PartialView("_BasketCartPartial", basketVM);
        }

        public IActionResult RemoveBasket(int id)
        {
            var basket = Request.Cookies["basket"];

            if (basket == null)
                return NotFound();

            List<BasketCookieItemViewModel> basketItems = JsonConvert.DeserializeObject<List<BasketCookieItemViewModel>>(basket);

            BasketCookieItemViewModel item = basketItems.Find(x => x.BookId == id);

            if (item == null)
                return NotFound();

            basketItems.Remove(item);

            Response.Cookies.Append("basket", JsonConvert.SerializeObject(basketItems));

            decimal totalPrice = 0;
            foreach (var bi in basketItems)
            {
                var book = _context.Books.Include(x => x.BookImages.Where(x => x.PosterStatus == true)).FirstOrDefault(x => x.Id == bi.BookId);
                var price = book.DiscountPercent > 0 ? (book.SalePrice * (100 - book.DiscountPercent) / 100) : book.SalePrice;
                totalPrice += (price * bi.Count);
            }

            return Ok(new {count=basketItems.Count,totalPrice=totalPrice.ToString("0.00")});
        }
        public IActionResult ShowBasket()
        {
            var basket = HttpContext.Request.Cookies["basket"];
            var basketItems = JsonConvert.DeserializeObject<List<BasketCookieItemViewModel>>(basket);

            return Json(basketItems);
        }
    }
}
