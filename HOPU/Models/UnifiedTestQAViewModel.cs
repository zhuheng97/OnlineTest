﻿namespace HOPU.Models
{
    /// <summary>
    /// 统测考试的model
    /// </summary>
    public class UnifiedTestQAViewModel
    {
        public string UserAnswer { get; set; }
        public string RealAnswer { get; set; }
        public bool IsTrue { get; set; }

    }

    public class UnifiedTestNewTopicIdViewModel
    {
        public int? UtId { get; set; }

        public string TopicID { get; set; }

        public string Title { get; set; }

        public string AnswerA { get; set; }

        public string AnswerB { get; set; }

        public string AnswerC { get; set; }

        public string AnswerD { get; set; }

        public string Answer { get; set; }

        public string CourseID { get; set; }
    }
}