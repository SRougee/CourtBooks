using System;
using System.Collections.Generic;
using System.Text;

namespace CourtBooks.Core
{
    public class Lesson
    {
        public string Date { get; set; }
        public string TimeStart { get; set; }
        public string TimeEnd { get; set; }
        public double LessonDuration { get; set; }
        public string Location { get; set; }
        public string Court { get; set; }
        public string LessonType { get; set; }


        public Lesson() 
        {
        
        }

        public static class LessonDetails
        {
            public static List<Lesson> tblLessons { get; private set; } = new List<Lesson>();

            public static void ClearAll()
            {
                tblLessons.Clear();
            }
        }
    }
}
