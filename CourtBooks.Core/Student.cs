using System;
using System.Collections.Generic;
using System.Text;

namespace CourtBooks.Core
{
    public class Student
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public string DominantHand { get; set; }
        public enum SkillLevel { Beginner, Intermediate, Advanced, Tournament }

        public Student() 
        { 
            
        }

        public static class StudentDetails
        {
            public static List<Student> tblStudents { get; private set; } = new List<Student>();

            public static void ClearAll()
            {
                tblStudents.Clear();
            }
        }
    }
}
