using System.Numerics;
using OpenTabletDriver;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.DependencyInjection;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;
using System;
using System.Linq;

namespace CursorIntegrate
{
    [PluginName("CursorIntegrator")]
    public class CursorIntegrate : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public CursorIntegrate() : base()
        {
        }

        public override PipelinePosition Position => PipelinePosition.PostTransform;

        protected Vector2 ToUnit(Vector2 input)
        {
            return new Vector2(
                (2 * input.X / width) - 1,
                (2 * input.Y / width) - ratio
            );
        }

        protected Vector2 FromUnit(Vector2 input)
        {
            return new Vector2(
                (width * (input.X + 1) / 2),
                (width * (input.Y + ratio) / 2)
            );
        }

        [Property("Screen width"), DefaultPropertyValue(1920), ToolTip
        (
            "The width of your screen in pixels"
        )]
        public int ScreenWidth
        {
            get { return width; }
            set { width = value; ratio = height / (float)width; }
        }
        private int width;

        [Property("Screen height"), DefaultPropertyValue(1080), ToolTip
        (
            "The height of your screen in pixels"
        )]
        public int ScreenHeight
        {
            get { return height; }
            set { height = value; ratio = height / (float)width; }
        }
        private int height;

        private float ratio;

        [Property("Input Sensitivity"), DefaultPropertyValue(0.02f)]
        public float InputSensitivity
        {
            get { return insens; }
            set { insens = value; }
        }
        private float insens;

        [Property("Output Sensitivity"), DefaultPropertyValue(1f), ToolTip
        (
            "Use input sensitivity"
        )]
        public float OutputSensitivity
        {
            get { return outsens; }
            set { outsens = value; }
        }
        private float outsens;

        [Property("Apply acceleration"), DefaultPropertyValue(false), ToolTip
        (
            "Not recommended"
        )]
        public Boolean ApplyAcceleration
        {
            get { return accel; }
            set { accel = value; }
        }
        private Boolean accel;

        [Property("Acceleration Exponent"), DefaultPropertyValue(2f)]
        public float AccelerationExponent
        {
            get { return exp; }
            set { exp = value; }
        }
        private float exp;

        [Property("Apply acceleration component wise"), DefaultPropertyValue(false)]
        public Boolean ApplyAccelerationComponent
        {
            get { return component; }
            set { component = value; }
        }
        private Boolean component;

        protected override void UpdateState()
        {
            //float alpha = (float)(reportStopwatch.Elapsed.TotalSeconds * Frequency / reportMsAvg);

            //if (State is ITiltReport tiltReport)
            //{
            //    tiltReport.Tilt = Vector2.Lerp(previousTiltTraget, tiltTraget, alpha);
            //    State = tiltReport;
            //}

            if (State is ITabletReport report && PenIsInRange())
            {
                //var lerp1 = Vector3.Lerp(previousTarget, controlPoint, alpha);
                //var lerp2 = Vector3.Lerp(controlPoint, target, alpha);
                //var res = Vector3.Lerp(lerp1, lerp2, alpha);
                if (accel) {
                    if (component)
                    {
                        position += new Vector2((float)Math.CopySign(Math.Pow(Math.Abs(velocity.X), exp), velocity.X), (float)Math.CopySign(Math.Pow(Math.Abs(velocity.Y), exp), velocity.Y));
                    } else {
                        position += (float) Math.Pow(velocity.Length(), exp - 1) * velocity;
                    }
                } else {
                    position += velocity;
                }
                
                report.Position = FromUnit(position * outsens);
                //report.Pressure = report.Pressure == 0 ? 0 : (uint)(res.Z);
                State = report;
                OnEmit();
            }
        }

        protected override void ConsumeState()
        {
            //if (State is ITiltReport tiltReport)
            //{
            //    if (!vec2IsFinite(tiltTraget)) tiltTraget = tiltReport.Tilt;
            //    previousTiltTraget = tiltTraget;
            //    tiltTraget += tiltWeight * (tiltReport.Tilt - tiltTraget);
            //}

            if (State is ITabletReport report)
            {
                var consumeDelta = (float)reportStopwatch.Restart().TotalMilliseconds;
                if (consumeDelta < 150)
                    reportMsAvg += ((consumeDelta - reportMsAvg) * 0.1f);

                //emaTarget = vec2IsFinite(emaTarget) ? emaTarget : report.Position;
                //emaTarget += emaWeight * (report.Position - emaTarget);

                //controlPoint = controlPointNext;
                //controlPointNext = new Vector3(emaTarget, report.Pressure);

                //previousTarget = target;
                //target = Vector3.Lerp(controlPoint, controlPointNext, 0.5f);

                velocity = ToUnit(report.Position) * insens;
            }
            else OnEmit();
        }

        //private Vector2 emaTarget, tiltTraget, previousTiltTraget;
        //private Vector3 controlPointNext, controlPoint, target, previousTarget;
        private Vector2 position;
        private Vector2 velocity;
        private HPETDeltaStopwatch reportStopwatch = new HPETDeltaStopwatch();
        private float reportMsAvg = 5;

        private bool vec2IsFinite(Vector2 vec) => float.IsFinite(vec.X) & float.IsFinite(vec.Y);
    }
}
