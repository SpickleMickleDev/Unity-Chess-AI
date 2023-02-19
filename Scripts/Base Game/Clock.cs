using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ChessAI
{
    class Clock
    {
        float cutOffPoint = 0.1f;
        public bool keepRunning;
        public int startTime;
        public float timeLeft;

        void Start()
        {
            timeLeft = startTime;
        }

        void Update()
        {
            if (keepRunning)
            {
                timeLeft -= Time.deltaTime;
                if (timeLeft < cutOffPoint)
                {
                    keepRunning = false;
                    timeLeft = 0f;
                    
                }
            }
        }
    }
}
