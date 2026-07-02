using System.Collections.Generic;

namespace Bookstore.Core.Models
{
    // A POCO
    public class Book
    {
        public string Isbn { get; set; }
        public string Title { get; set; }
        public string Language { get; set; }// title's lang attribute
        public IList<string> Authors { get; set; } = new List<string>();
        public int Year { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public string Cover { get; set; }                

    
        public string AuthorsDisplay
        {
            get { return string.Join(", ", Authors); }
        }
    }
}