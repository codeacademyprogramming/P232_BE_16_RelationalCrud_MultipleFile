﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pustok.DAL;
using Pustok.Helpers;
using Pustok.Models;
using Pustok.ViewModels;

namespace Pustok.Areas.manage.Controllers
{
    [Area("manage")]
    public class BookController : Controller
    {
        private readonly PustokDbContext _context;
        private readonly IWebHostEnvironment _env;

        public BookController(PustokDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }
        public IActionResult Index()
        {
            var data = _context.Books
                .Include(x => x.BookImages)
                .Include(x => x.Genre)
                .Where(x=>!x.IsDeleted)
                .ToList();
            return View(data);
        }

        public IActionResult Create()
        {
            ViewBag.Authors = _context.Authors.ToList();
            ViewBag.Genres = _context.Genres.ToList();
            ViewBag.Tags = _context.Tags.ToList();

            return View();
        }

        [HttpPost]
        public IActionResult Create(Book book)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Authors = _context.Authors.ToList();
                ViewBag.Genres = _context.Genres.ToList();

                return View();
            }

            if (!_context.Authors.Any(x => x.Id == book.AuthorId))
            {
                ModelState.AddModelError("AuthorId", "Auhtor not found");
                return View();
            }

            if (!_context.Genres.Any(x => x.Id == book.GenreId))
            {
                ModelState.AddModelError("GenreId", "Genre not found");
                return View();
            }

            if (book.PosterFile == null)
                ModelState.AddModelError("PosterFile", "PosterFile is required");

            if (book.HoverPosterFile == null)
                ModelState.AddModelError("HoverPosterFile", "HoverPosterFile is required");

            if (book != null && book.PosterFile.ContentType != "image/jpeg" && book.PosterFile.ContentType != "image/png")
                ModelState.AddModelError("PosterFile", "PosterFile must be image/png or image/jpeg");

            if (book != null && book.PosterFile.Length > 2097152)
                ModelState.AddModelError("PosterFile", "PosterFile must be less or equal than 2MB");

            if (book != null && book.HoverPosterFile.ContentType != "image/jpeg" && book.HoverPosterFile.ContentType != "image/png")
                ModelState.AddModelError("HoverPosterFile", "HoverPosterFile must be image/png or image/jpeg");

            if (book != null && book.HoverPosterFile.Length > 2097152)
                ModelState.AddModelError("HoverPosterFile", "HoverPosterFile must be less or equal than 2MB");

            foreach (var item in book.ImageFiles)
            {
                if (item.ContentType != "image/jpeg" && item.ContentType != "image/png")
                    ModelState.AddModelError("ImageFiles", "ImageFiles must be image/png or image/jpeg");

                if (item.Length > 2097152)
                    ModelState.AddModelError("ImageFiles", "ImageFiles must be less or equal than 2MB");
            }


            foreach (var tagId in book.TagIds)
            {
                if (!_context.Tags.Any(x => x.Id == tagId))
                {
                    ModelState.AddModelError("TagIds", "Tag not found");
                    break;
                }
            }


            if (!ModelState.IsValid)
            {
                ViewBag.Authors = _context.Authors.ToList();
                ViewBag.Genres = _context.Genres.ToList();

                return View();
            }



            book.BookTags = book.TagIds.Select(x => new BookTag { TagId = x }).ToList();


            BookImage posterBi = new BookImage
            {
                Image = FileManager.Save(book.PosterFile, _env.WebRootPath + "/uploads/books"),
                PosterStatus = true,
            };

            BookImage hoverPosterBi = new BookImage
            {
                Image = FileManager.Save(book.HoverPosterFile, _env.WebRootPath + "/uploads/books"),
                PosterStatus = false,
            };

            book.BookImages.Add(posterBi);
            book.BookImages.Add(hoverPosterBi);

            foreach (var biFile in book.ImageFiles)
            {
                BookImage bi = new BookImage
                {
                    Image = FileManager.Save(biFile, _env.WebRootPath + "/uploads/books"),
                    PosterStatus = null,
                };

                book.BookImages.Add(bi);
            }



            book.CreatedAt = DateTime.UtcNow;

            _context.Books.Add(book);
            _context.SaveChanges();

            return RedirectToAction("index");
        }

        public IActionResult Edit(int id)
        {
            Book book = _context.Books
                .Include(x => x.BookTags)
                .Include(x => x.BookImages)
                .FirstOrDefault(x => x.Id == id && !x.IsDeleted);

            if (book == null)
                return View("Error");

            book.TagIds = book.BookTags.Select(x => x.TagId).ToList();

            ViewBag.Authors = _context.Authors.ToList();
            ViewBag.Genres = _context.Genres.ToList();
            ViewBag.Tags = _context.Tags.ToList();

            return View(book);
        }

        [HttpPost]
        public IActionResult Edit(Book book)
        {

            return Ok(book);
        }


        public IActionResult Delete(int id)
        {
            Book book = _context.Books.FirstOrDefault(x => x.Id == id);

            if (book == null)
                return NotFound();

            book.IsDeleted = true;
            _context.SaveChanges();

            return Ok();
        }
    }
}
