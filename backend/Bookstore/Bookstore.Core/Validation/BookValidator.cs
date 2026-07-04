using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bookstore.Core.Common;
using Bookstore.Core.Models;

namespace Bookstore.Core.Validation
{
    // Enforces the "no empty / no invalid objects" data-integrity rule before a
    // Book is ever written to the XML store. Collects every violation instead
    // of stopping at the first, so a caller sees the whole problem in one
    // round trip rather than fixing fields one at a time.
    public static class BookValidator
    {
        // ISBN-13 format only: exactly 13 digits. We deliberately do NOT verify
        // the checksum, because the assignment's own sample ISBNs are not
        // checksum-valid, so enforcing it would reject the seed data itself.
        private static readonly Regex Isbn13 = new Regex(@"^\d{13}$");

        public static Result Validate(Book book)
        {
            if (book == null)
                return Result.Fail(ResultError.ValidationFailed,
                    "Book must not be null (caller error, not user input).");

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(book.Isbn) || !Isbn13.IsMatch(book.Isbn))
                errors.Add("ISBN must be exactly 13 digits.");

            if (string.IsNullOrWhiteSpace(book.Title))
                errors.Add("Title is required.");

            if (string.IsNullOrWhiteSpace(book.Language))
                errors.Add("Language (title lang) is required.");

            if (book.Authors == null || book.Authors.Count == 0 ||
                book.Authors.Any(string.IsNullOrWhiteSpace))
                errors.Add("At least one non-empty author is required.");

            if (book.Year <= 0)
                errors.Add("Year must be a positive number.");

            if (book.Price < 0)
                errors.Add("Price cannot be negative.");

            if (string.IsNullOrWhiteSpace(book.Category))
                errors.Add("Category is required.");

            return errors.Count == 0 ? Result.Ok() : Result.Fail(ResultError.ValidationFailed, errors);
        }
    }
}
