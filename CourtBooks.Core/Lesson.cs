using System;
using System.Collections.Generic;
using System.Text;

namespace CourtBooks.Core
{
    public class Lesson
    {
        public string Datetime { get; set; }
        public string Location { get; set; }
        public string Court { get; set; }
        public string LessonType { get; set; }


        public Lesson() 
        {
        
        }
    }
}
