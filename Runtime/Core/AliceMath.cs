using ProjectAlice.Runtime.Exception;
using System.Collections.Generic;

namespace ProjectAlice.Runtime.Core
{
    public static class AliceMath
    {
        public readonly static AliceNumber zero = new AliceNumber(0);

        public readonly static AliceNumber Pi = NE(3, 14159265359);

        public readonly static AliceNumber Pi_Half = NE(1, 57079632679);

        public readonly static AliceNumber E = NE(2, 71828182846);

        public readonly static AliceNumber MaxValue = new AliceNumber(AliceNumber.PRECISION - 1, AliceNumber.PRECISION - 1);

        public readonly static AliceNumber MinValue = new AliceNumber(-AliceNumber.PRECISION, 0);
        public static AliceNumber Lerp(AliceNumber a, AliceNumber b, AliceNumber progress)
        {
            return (b - a) * progress + a;
        }
        public static AliceNumber Sqrt(AliceNumber a)
        {
            if (a < 0)
                throw new AliceException("[Alice Error] Sqrt(x) should take x >= 0.");

            if (a == zero)
                return zero;

            var estimate_log = EstimateLog10(a);
            estimate_log /= 2;

            AliceNumber test;
            if (estimate_log >= 0)
            {
                int int_start = 1;
                for (int i = 0; i < estimate_log; ++i) int_start *= 10;
                test = new AliceNumber(int_start);
            }
            else
            {
                int int_start = 1;
                for (int i = 0; i < AliceNumber.LOG_PRECISION + estimate_log; ++i) int_start *= 10;
                test = new AliceNumber(0, int_start);
            }

            // newton
            const int NEWTON_PRECISION = 8;
            for (int i = 0; i < NEWTON_PRECISION; ++i)
            {
                test = (test * test + a) / (2 * test);
            }

            return test;
        }
        public static AliceNumber Exp(AliceNumber a)
        {
            const int PRECISION = 12;

            int pow = 1;
            for (int i = 0; i < PRECISION; ++i)
                pow = pow * 2;

            AliceNumber num = 1 + a / pow;
            for (int i = 0; i < PRECISION; ++i)
                num = num * num;

            return num;
        }
        public static AliceNumber Log(AliceNumber a)
        {
            if (a <= 0)
                throw new AliceException("[Alice Error] Log(x) should take x > 0.");

            // Normalize (a) to range sqrt2/2 ~ sqrt2
            var k = 0;
            if (a > Sqrt2)
            {
                var ref_num = Sqrt2;
                k = 1;
                var factor = 2;
                while (true)
                {
                    var test = ref_num * 2;
                    if (test > a || test.Overflow)
                        break;

                    ref_num = test;
                    k++;
                    factor *= 2;
                }

                a /= factor;
            }
            else if (a < Sqrt2_half)
            {
                var ref_num = Sqrt2_half;

                while (true)
                {
                    a *= 2;
                    k--;

                    if (a > ref_num)
                        break;
                }
            }

            // Change variable
            var f = a - 1;
            var s = f / (2 + f);
            var s2 = s * s;
            var s4 = s2 * s2;
            var t1 = s2 * (L1 + s4 * (L3 + s4 * (L5 + s4 * L7)));
            var t2 = s4 * (L2 + s4 * (L4 + s4 * L6));
            var R = t1 + t2;
            var hfsq = NE(0, 5) * f * f;

            return k * Log2 - ((hfsq - (s * (hfsq + R))) - f);
        }
        public static AliceNumber Sin(AliceNumber radians)
        {
            return CordicTrigonometric(radians).sin;
        }
        public static AliceNumber Cos(AliceNumber radians)
        {
            return CordicTrigonometric(radians).cos;
        }
        public static AliceNumber Tan(AliceNumber radians)
        {
            (var sin, var cos) = CordicTrigonometric(radians);
            if (cos == zero)
                return sin > 0 ? MaxValue : MinValue;

            return sin / cos;
        }
        public static AliceNumber ArcSin(AliceNumber sin)
        {
            if (sin > 1 || sin < -1)
                throw new AliceException("[Alice Error] ArcSin should take range in -1 ~ 1.");

            var cos = Sqrt(1 - sin * sin);
            return CordicInverseTrigonometric(cos, sin);
        }
        public static AliceNumber ArcCos(AliceNumber cos)
        {
            if (cos > 1 || cos < -1)
                throw new AliceException("[Alice Error] ArcCos should take range in -1 ~ 1.");

            var sin = Sqrt(1 - cos * cos);
            return CordicInverseTrigonometric(cos, sin);
        }
        public static AliceNumber Arg(AliceNumber y, AliceNumber x)
        {
            if (x == 0 && y == 0)
                throw new AliceException("[Alice Error] Arg cannot take zero point.");

            var magnitude = Sqrt(x * x + y * y);
            var cos = x / magnitude;
            var sin = y / magnitude;
            return CordicInverseTrigonometric(cos, sin);
        }
        public static AliceNumber NormalizePi(AliceNumber radians)
        {
            var inte = radians.Integer * radians.SignFactor;
            if (inte > 3)
            {
                var large_inte = inte * AliceNumber.PRECISION;
                var large_pi = Pi.Integer * AliceNumber.PRECISION + Pi.Fraction;
                radians -= Pi * (int)(large_inte / (2 * large_pi) * radians.SignFactor * 2);
            }

            while (radians > Pi)
                radians -= 2 * Pi;

            while (radians < -Pi)
                radians += 2 * Pi;

            return radians;
        }

        #region Internal

        private static AliceNumber NE(long integer, long normalFraction) => AliceNumber.NormalExpression(integer, normalFraction);
        private static AliceNumber SE(long fullNumber, int exp) => AliceNumber.ScientificExpression(fullNumber, exp);

        #region Log Const
        private static AliceNumber Sqrt2 = NE(1, 41421356237);
        private static AliceNumber Sqrt2_half = NE(0, 70710678118);
        private static AliceNumber Log2 = NE(0, 693_147_180_369);
        private static AliceNumber L1 = NE(0, 666_666_666_666);
        private static AliceNumber L2 = NE(0, 399_999_999_999);
        private static AliceNumber L3 = NE(0, 285_714_287_436);
        private static AliceNumber L4 = NE(0, 222_221_984_321);
        private static AliceNumber L5 = NE(0, 181_835_721_616);
        private static AliceNumber L6 = NE(0, 153_138_376_992);
        private static AliceNumber L7 = NE(0, 147_981_986_051);
        #endregion

        #region Cordic Const

        private static List<(AliceNumber radians, AliceNumber tangentValue, AliceNumber normalizedFactor)> cordic_table =
            new List<(AliceNumber tangentValue, AliceNumber radians, AliceNumber normalizedFactor)>
            {
                (SE(1, 0),          SE(785398163, -1), NE(0, 707106781)), // 1
                (SE(5, -1),         SE(463647609, -1), NE(0, 632455532)), // 1/2
                (SE(25, -1),        SE(244978663, -1), NE(0, 613571991)), // 1/4
                (SE(125, -1),       SE(124354994, -1), NE(0, 608833912)), // 1/8
                (SE(625, -2),       SE(624188100, -2), NE(0, 607648256)), // 1/16
                (SE(3125, -2),      SE(312398334, -2), NE(0, 607351770)), // 1/32
                (SE(15625, -2),     SE(156237286, -2), NE(0, 607277644)), // 1/64
                (SE(78125, -3),     SE(781234106, -3), NE(0, 607259112)), // 1/128
                (SE(390625, -3),    SE(390623013, -3), NE(0, 607254479)), // 1/256
                (SE(1953125, -3),   SE(195312251, -3), NE(0, 607253321)), // 1/512
                (SE(9765625, -4),   SE(976562189, -4), NE(0, 607253031)), // 1/1024
                (SE(48828125, -4),  SE(488281211, -4), NE(0, 607252959)), // 1/2048
                (SE(244140625, -4), SE(244140620, -4), NE(0, 607252941)), // 1/4096
                (SE(122070312, -4), SE(122070312, -4), NE(0, 607252936)), // 1/8192
                (SE(610351562, -5), SE(609979260, -5), NE(0, 607252935)), // 1/16394
                (SE(305175781, -5), SE(304896640, -5), NE(0, 607252934)), // 1/32798
            };

        private static bool cordic_prepared = false;

        private static int cordic_idx;
        private static AliceNumber cordic_precision;

        private static void PrepareCordic()
        {
            if (cordic_prepared)
                return;

            cordic_idx = 0;
            for (int i = 0; i < cordic_table.Count; ++i)
            {
                if (cordic_table[i].tangentValue == zero || cordic_table[i].radians == zero)
                    break;

                cordic_idx++;
            }

            if (cordic_idx == 0)
                throw new AliceException("[Alice Error] Too low precision for CORDIC.");

            cordic_precision = cordic_table[cordic_idx - 1].radians / 2;
            cordic_prepared = true;
        }

        private static (AliceNumber cos, AliceNumber sin) CordicTrigonometric(AliceNumber radians)
        {
            PrepareCordic();

            // Normalize radians to -Pi ~ Pi
            radians = NormalizePi(radians);

            // Special case
            if (radians == Pi || radians == -Pi)
                return (new AliceNumber(-1), zero);
            if (radians == Pi_Half)
                return (zero, new AliceNumber(1));
            if (radians == -Pi_Half)
                return (zero, new AliceNumber(-1));
            if (radians == zero)
                return (new AliceNumber(1), zero);

            // To first quadrand
            int quadrand;
            if (radians > Pi_Half)
            {
                quadrand = 2;
                radians = Pi - radians;
            }
            else if (radians > zero)
            {
                quadrand = 1;
            }
            else if (radians > -Pi_Half)
            {
                quadrand = -1;
                radians = -radians;
            }
            else
            {
                quadrand = -2;
                radians = Pi + radians;
            }


            var cos = new AliceNumber(1);
            var sin = new AliceNumber(0);
            var factor = new AliceNumber(1);

            for (int i = 0; i < cordic_idx; ++i)
            {
                if (radians < cordic_precision && radians > -cordic_precision) break;

                var sign = radians < zero ? -1 : 1;
                (var tan, var dr, var f) = cordic_table[i];

                radians -= sign * dr;
                (cos, sin) = (cos - sign * tan * sin, sin + sign * tan * cos);
                factor = f;
            }

            return quadrand switch
            {
                2 => (-cos * factor, sin * factor),
                1 => (cos * factor, sin * factor),
                -1 => (cos * factor, -sin * factor),
                -2 => (-cos * factor, -sin * factor),
                _ => (cos * factor, sin * factor)
            };
        }

        private static AliceNumber CordicInverseTrigonometric(AliceNumber cos, AliceNumber sin)
        {
            PrepareCordic();

            var delta_phase = zero;
            if (cos < 0)
            {
                delta_phase = sin > 0 ? Pi : -Pi;
                cos = -cos;
                sin = -sin;
            }

            var radians = zero;
            for (int i = 0; i < cordic_idx; ++i)
            {
                var sign = sin < zero ? -1 : 1;
                (var tan, var dr, var f) = cordic_table[i];

                radians += sign * dr;
                (cos, sin) = (cos + sign * tan * sin, sin - sign * tan * cos);

                var test = f * sin;
                if (test < cordic_precision && test > -cordic_precision) break;
            }

            return radians + delta_phase;
        }

        #endregion

        private static int EstimateLog10(AliceNumber a)
        {
            var integer = a.Integer;
            var fraction = a.Fraction;
            var sign = a.SignFactor;

            bool larger_than_one = integer != 0;
            var target = larger_than_one ? integer * sign : fraction * sign;

            int result = 0;
            long reference = 1;

            while (reference <= target)
            {
                result += 1;
                reference *= 10;
            }

            result -= larger_than_one ? 0 : AliceNumber.LOG_PRECISION;

            return result;
        }

        #endregion
    }
}
