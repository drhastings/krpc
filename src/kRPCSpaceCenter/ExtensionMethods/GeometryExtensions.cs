using System;
using UnityEngine;

namespace KRPCSpaceCenter.ExtensionMethods
{
    public static class GeometryExtensions
    {
        /// <summary>
        /// Convert a Unity3d vector to a protocol buffer vector
        /// </summary>
        public static KRPC.Utils.Tuple<double,double,double> ToTuple (this Vector3d v)
        {
            return new KRPC.Utils.Tuple<double,double,double> (v.x, v.y, v.z);
        }

        /// <summary>
        /// Convert a Unity3d vector to a protocol buffer vector
        /// </summary>
        public static KRPC.Utils.Tuple<double,double,double> ToTuple (this Vector3 v)
        {
            return new KRPC.Utils.Tuple<double,double,double> (v.x, v.y, v.z);
        }

        /// <summary>
        /// Convert a protocol buffer vector to a Unity3d vector
        /// </summary>
        public static Vector3d ToVector (this KRPC.Utils.Tuple<double,double,double> v)
        {
            return new Vector3d (v.Item1, v.Item2, v.Item3);
        }

        /// <summary>
        /// Compute the sign of a vector's elements
        /// </summary>
        public static Vector3d Sign (this Vector3d v)
        {
            return new Vector3d (Math.Sign (v.x), Math.Sign (v.y), Math.Sign (v.z));
        }

        /// <summary>
        /// Raise the elements of a vector's elements to the given exponent
        /// </summary>
        public static Vector3d Pow (this Vector3d v, double e)
        {
            return new Vector3d (Math.Pow (v.x, e), Math.Pow (v.y, e), Math.Pow (v.z, e));
        }

        /// <summary>
        /// Compute the element-wise inverse of a vector
        /// </summary>
        public static Vector3d Inverse (this Vector3d v)
        {
            return new Vector3d (1d / v.x, 1d / v.y, 1d / v.z);
        }

        /// <summary>
        /// Normalize an angle
        /// </summary>
        public static double normAngle (double angle)
        {
            return angle - 360d * Math.Floor ((angle + 180d) / 360d);
        }

        /// <summary>
        /// Apply normAngle element-wise to a vector
        /// </summary>
        public static Vector3d ReduceAngles (this Vector3d v)
        {
            return new Vector3d (normAngle (v.x), normAngle (v.y), normAngle (v.z));
        }

        /// <summary>
        /// Round all values in a vector to the given precision
        /// </summary>
        public static Vector3 Round (this Vector3 v, int decimalPlaces)
        {
            // TODO: remove horrid casts
            return new Vector3 (
                (float)Math.Round ((double)v.x, decimalPlaces),
                (float)Math.Round ((double)v.y, decimalPlaces),
                (float)Math.Round ((double)v.z, decimalPlaces));
        }

        /// <summary>
        /// Clamp a value to the given range
        /// </summary>
        public static T Clamp<T> (this T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo (min) < 0)
                return min;
            else if (value.CompareTo (max) > 0)
                return max;
            else
                return value;
        }

        /// <summary>
        /// Clamps the given angle to the range [0,360]
        /// </summary>
        static double ClampAngleDegrees (double angle)
        {
            angle = angle % 360d;
            if (angle < 0d)
                angle += 360d;
            return angle;
        }

        /// <summary>
        /// Compute the pitch, heading and roll angle of a quaternion in degrees.
        /// </summary>
        public static Vector3d PitchHeadingRoll (this QuaternionD q)
        {
            // First, adjust the euler angle extraction order frmo z,y,x -> -y,z,x
            // i.e. to extra pitch, then heading, then roll
            // FIXME: QuaternionD.Euler method is not found at runtime
            var r = q * ((QuaternionD)Quaternion.Euler (-90f, 0f, 0f)); //QuaternionD.Euler (-90d, 0d, 0d);
            // FIXME: QuaternionD.eulerAngles property is not found at runtime
            Vector3d eulerAngles = ((Quaternion)r).eulerAngles;
            // Clamp angles to range (0,360)
            eulerAngles.x = ClampAngleDegrees (eulerAngles.x);
            eulerAngles.y = ClampAngleDegrees (eulerAngles.y);
            eulerAngles.z = ClampAngleDegrees (eulerAngles.z);
            // Convert angle around -y axis to pitch, with range [-90, 90]
            var pitch = eulerAngles.x > 180d ? 360d - eulerAngles.x : -eulerAngles.x;
            // Convert angle around z axis to heading, with range [0, 360]
            var heading = eulerAngles.y;
            // Convert angle around x axis to roll, with range [-180, 180]
            var roll = eulerAngles.z > 180d ? 360d - eulerAngles.z : -eulerAngles.z;
            return new Vector3d (pitch, heading, roll);
        }

        /// <summary>
        /// Compute the inverse quaternion. Assumes the input is a unit quaternion.
        /// </summary>
        public static QuaternionD Inverse (this QuaternionD q)
        {
            return new QuaternionD (-q.x, -q.y, -q.z, q.w);
        }

        /// <summary>
        /// Implementation of QuaternionD.OrthoNormalize, using stabilized Gram-Schmidt
        /// </summary>
        public static void OrthoNormalize2 (ref Vector3d normal, ref Vector3d tangent)
        {
            normal.Normalize ();
            tangent.Normalize (); // Additional normalization, avoids large tangent norm
            var proj = normal * Vector3d.Dot (tangent, normal);
            tangent = tangent - proj;
            tangent.Normalize ();
        }

        /// <summary>
        /// Compute the norm of a quaternion
        /// </summary>
        public static double Norm (this QuaternionD q)
        {
            return Math.Sqrt (q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        }

        /// <summary>
        /// Normalize a quaternion
        /// </summary>
        public static QuaternionD Normalize (this QuaternionD q)
        {
            var sf = 1d / q.Norm ();
            return new QuaternionD (q.x * sf, q.y * sf, q.z * sf, q.w * sf);
        }

        /// <summary>
        /// Implementation of QuaternionD.LookRotation
        /// </summary>
        public static QuaternionD LookRotation2 (Vector3d forward, Vector3d up)
        {
            OrthoNormalize2 (ref forward, ref up);
            Vector3d right = Vector3d.Cross (up, forward);
            var w = Math.Sqrt (1.0d + right.x + up.y + forward.z) * 0.5d;
            var r = 0.25d / w;
            var x = (up.z - forward.y) * r;
            var y = (forward.x - right.z) * r;
            var z = (right.y - up.x) * r;
            return new QuaternionD (x, y, z, w);
        }
    }
}
