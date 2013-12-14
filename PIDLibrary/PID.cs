using System;

namespace PIDLibrary
{
    public class PID
    {
        /*working variables*/
        long lastTime;
        double Setpoint;
        double ITerm, lastInput;
        double kp, ki, kd;
        long SampleTime = 1000;  //1 sec
        double outMin, outMax;
        double lastOutput;

        public PID(double Kp, double Ki, double Kd, double setPoint, long sampleTime, double outputMin, double outputMax)
        {
            kp = Kp;
            ki = Ki;
            kd = Kd;

            Setpoint = setPoint;
            outMin = outputMin;
            outMax = outputMax;

            //sample frequency passed in in milliseconds
            SampleTime = sampleTime;

            SetTunings(Kp, Ki, Kd);

            lastTime = DateTime.Now.Subtract(new TimeSpan(0, 0, 0, 0, (int)sampleTime)).Ticks;


        }



        public double Compute(double input)
        {
            double output;
            long now = DateTime.Now.Ticks;
            long timeChange = (now - lastTime);

            //double scaledInput = ScaleValue(input, inMin, inMax, 0, 1);
            //double scaledSetPoint = ScaleValue(Setpoint, inMin, inMax, 0, 1);

            long sampleTimeTicks = (new TimeSpan(0, 0, 0, 0, (int)SampleTime)).Ticks;

            if (timeChange >= sampleTimeTicks)
            {
                /*Compute all the working error variables*/
                double error = Setpoint - input;
                ITerm += (ki * error);
                if (ITerm > outMax) ITerm = outMax;
                else if (ITerm < outMin) ITerm = outMin;
                double dInput = (input - lastInput);

                /*Compute PID Output*/
                output = kp * error + ITerm - kd * dInput;
                if (output > outMax)
                    output = outMax;
                else if (output < outMin)
                    output = outMin;

                /*Remember some variables for next time*/
                lastInput = input;
                lastTime = now;
                lastOutput = output;

                return output;
            }

            return lastOutput;
        }

        void SetTunings(double Kp, double Ki, double Kd)
        {
            if (Kp < 0 || Ki < 0 || Kd < 0) return;

            double SampleTimeInSec = ((double)SampleTime) / 1000;
            kp = Kp;
            ki = Ki * SampleTimeInSec;
            kd = Kd / SampleTimeInSec;
        }

    }
}
