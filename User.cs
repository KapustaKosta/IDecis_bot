using System;
using System.Collections.Generic;
using System.Text;

namespace IDecisBot
{
    class User
    {
        public long chatID;

        public bool inPool;
        public int poolQuestionNum;
        public int criteriaNum;
        public List<int[]> poolResults;

        public User()
        {
            chatID = -1;

            inPool = false;
            poolQuestionNum = 0;
            criteriaNum = 0;
            poolResults = new List<int[]>();
        }
    }
}
