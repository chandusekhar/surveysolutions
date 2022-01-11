﻿using System;
using System.Collections.Generic;

namespace WB.Core.BoundedContexts.Designer.ImportExport.Models.Question
{
    public class MultiOptionsQuestion : AbstractQuestion, ICategoricalQuestion, ILinkedQuestion
    {
        public bool AreAnswersOrdered { get; set; }
        public int? MaxAllowedAnswers { get; set; }
        public MultiOptionsDisplayMode DisplayMode { get; set; }
        public Guid? CategoriesId { get; set; }
        public List<Answer>? Answers { get; set; }
        public string? LinkedTo { get; set; }
        public string? FilterExpression { get; set; }
    }

    public enum MultiOptionsDisplayMode
    {
        Checkboxes,
        YesNo,
        Combobox,
    }
}
