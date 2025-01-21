using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Pages
{
    public class InputModel : PageModel
    {
        private List<(string, string, string)> _triples = new List<(string, string, string)>();

        [BindProperty]
        public string Subject { get; set; }

        [BindProperty]
        public string Relation { get; set; }

        [BindProperty]
        public string Object { get; set; }

        public List<(string, string, string)> Triples => _triples;

        public void OnPost()
        {
            _triples.Add((Subject, Relation, Object));

            // Clear input fields
            Subject = "";
            Relation = "";
            Object = "";
        }
    }
}
