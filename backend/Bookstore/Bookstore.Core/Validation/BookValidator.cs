using System;
using System.Linq;
using System.Text.RegularExpressions;
using Bookstore.Core.Models;

namespace Bookstore.Core.Validation
{
    /// <summary>
    /// Enforces the "no empty / no invalid objects" data-integrity rule before a
    /// Book is ever written to the XML store. Throws ArgumentException (which the
    /// API layer maps to HTTP 400) describing the first violation found.
    /// </summary>
    public static class BookValidator
    {
        // ISBN-13 format only: exactly 13 digits. We deliberately do NOT verify
        // the checksum, because the assignment's own sample ISBNs are not
        // checksum-valid, so enforcing it would reject the seed data itself.
        private static readonly Regex Isbn13 = new Regex(@"^\d{13}$");

        public static void Validate(Book book)
        {
            if (book == null)
                throw new ArgumentNullException(nameof(book));

            if (string.IsNullOrWhiteSpace(book.Isbn) || !Isbn13.IsMatch(book.Isbn))
                throw new ArgumentException("ISBN must be exactly 13 digits.", nameof(book.Isbn));

            if (string.IsNullOrWhiteSpace(book.Title))
                throw new ArgumentException("Title is required.", nameof(book.Title));

            if (string.IsNullOrWhiteSpace(book.Language))
                throw new ArgumentException("Language (title lang) is required.", nameof(book.Language));

            if (book.Authors == null || book.Authors.Count == 0 ||
                book.Authors.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("At least one non-empty author is required.", nameof(book.Authors));

            if (book.Year <= 0)
                throw new ArgumentException("Year must be a positive number.", nameof(book.Year));

            if (book.Price < 0)
                throw new ArgumentException("Price cannot be negative.", nameof(book.Price));

            if (string.IsNullOrWhiteSpace(book.Category))
                throw new ArgumentException("Category is required.", nameof(book.Category));
        }
    }
}
