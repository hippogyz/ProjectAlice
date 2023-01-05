
namespace ProjectAlice.Runtime.Core
{
    public struct AliceNumber
    {
        public bool Overflow { get; private set; }
        public long Integer => sign * unsign_inte;
        public long Fraction => sign * unsign_frac;
        public int SignFactor => Sign(Integer, Fraction);

        private readonly int sign;
        private readonly long unsign_inte;
        private readonly long unsign_frac;

        #region ctor

        public AliceNumber(int intValue)
        {
            int integer = intValue;
            int fraction = 0;

            Overflow = CheckOverflow(ref integer, ref fraction);

            sign = Sign(integer, fraction);
            unsign_inte = integer * sign;
            unsign_frac = fraction * sign;
        }

        public AliceNumber(float floatValue) // for test only
        {
            var float_sign = floatValue >= 0 ? 1 : -1;

            var integer = (long)(floatValue * float_sign);
            var fraction = (long)((floatValue * float_sign - integer) * PRECISION);

            integer *= float_sign;
            fraction *= float_sign;

            Overflow = CheckOverflow(ref integer, ref fraction);

            sign = Sign(integer, fraction);
            unsign_inte = integer * sign;
            unsign_frac = fraction * sign;
        }

        public AliceNumber(string strValue)
        {
            int integer;
            int fraction = 0;

            strValue = strValue.Trim();
            var split = strValue.Split('.');

            integer = int.Parse(split[0]);

            if (split.Length > 1)
            {
                var str_sign = split[0].StartsWith('-');
                var str_frac = split[1];

                if (str_frac.Length >= LOG_PRECISION)
                    str_frac = str_frac.Substring(0, LOG_PRECISION);

                fraction = int.Parse(str_frac);
                for (int i = 0; i < LOG_PRECISION - str_frac.Length; ++i)
                    fraction *= 10;

                fraction *= str_sign ? -1 : 1;
            }


            Overflow = CheckOverflow(ref integer, ref fraction);

            sign = Sign(integer, fraction);
            unsign_inte = integer * sign;
            unsign_frac = fraction * sign;
        }

        public AliceNumber(long integer, long fraction)
        {
            Carry(ref integer, ref fraction);
            Overflow = CheckOverflow(ref integer, ref fraction);
            sign = Sign(integer, fraction);
            unsign_inte = integer * sign;
            unsign_frac = fraction * sign;
        }

        private static bool CheckOverflow(ref int integer, ref int fraction)
        {
            long long_inte = (long)integer;
            long long_frac = (long)fraction;

            var overflow = CheckOverflow(ref long_inte, ref long_frac);

            integer = (int)long_inte;
            fraction = (int)long_frac;

            return overflow;
        }
        private static bool CheckOverflow(ref long integer, ref long fraction)
        {
            var sign = Sign(integer, fraction);

            var unsign_inte = integer * sign;

            if (unsign_inte >= PRECISION)
            {
                if (integer == -PRECISION && fraction == 0)
                    return false;

                var overflow_count = unsign_inte / PRECISION;
                var same_sign = overflow_count % 2 == 0;

                var mod = unsign_inte - overflow_count * PRECISION;

                if (same_sign)
                {
                    integer = mod * sign;
                }
                else
                {
                    integer = (mod - PRECISION) * sign;
                    fraction = (fraction - PRECISION) * sign;
                }

                return true;
            }

            return false;
        }
        private static int Sign(long integer, long fraction)
        {
            return integer != 0 ? (integer > 0 ? 1 : -1) : (fraction >= 0 ? 1 : -1);
        }
        private static void Carry(ref long inte, ref long frac)
        {
            long carry = 0;

            if (frac >= PRECISION)
            {
                carry = frac / PRECISION;
                frac -= carry * PRECISION;
            }
            else if (frac <= -PRECISION)
            {
                carry = -((-frac) / PRECISION);
                frac -= carry * PRECISION;
            }

            inte += carry;

            if (inte > 0 && frac < 0)
            {
                inte -= 1;
                frac += PRECISION;
            }
            else if (inte < 0 && frac > 0)
            {
                inte += 1;
                frac -= PRECISION;
            }
        }

        public static AliceNumber NormalExpression(long integer, long normalFraction)
        {
            if (normalFraction == 0)
                return new AliceNumber(integer, 0);

            var sign = Sign(integer, normalFraction);
            var unsign_frac = normalFraction > 0 ? normalFraction : -normalFraction;

            var is_multi_factor = unsign_frac < PRECISION;
            var factor = is_multi_factor ? 1 : 10;

            var small = is_multi_factor ? unsign_frac : PRECISION;
            var large = is_multi_factor ? PRECISION : unsign_frac;

            while (true)
            {
                small *= 10;
                if (is_multi_factor)
                {
                    if (small >= large)
                        break;
                }
                else
                {
                    if (small > large)
                        break;
                }
                factor *= 10;
            }

            if (factor != 1)
                unsign_frac = is_multi_factor ? unsign_frac * factor : unsign_frac / factor;

            return new AliceNumber(integer, sign * unsign_frac);
        }

        public static AliceNumber ScientificExpression(long fullNumber, int log)
        {
            if (log >= LOG_PRECISION)
                return new AliceNumber(PRECISION);

            if (log < -LOG_PRECISION)
                return new AliceNumber(0);


            var sign = fullNumber > 0 ? 1 : -1;
            var unsign_full_num = fullNumber * sign;

            var full_log = 0;
            long full_ref = 1;

            while (true)
            {
                if (full_ref > unsign_full_num)
                    break;

                full_log += 1;
                full_ref *= 10;
            }

            // 计算整数部分
            long inte = 0;
            var inte_log = full_log - log - 1;
            if (log < 0) // 纯小数，不计算整数部分
            {

            }
            if (inte_log > 0) // fullNumber含小数
            {
                long inte_factor = 1;
                for (int i = 0; i < inte_log; ++i)
                    inte_factor *= 10;

                inte = unsign_full_num / inte_factor;
                unsign_full_num -= inte * inte_factor;
            }
            else if (inte_log < 0) // fullNumber精度不够整数位，需要补位
            {
                long inte_factor = 1;
                for (int i = 0; i < -inte_log; ++i)
                    inte_factor *= 10;

                return new AliceNumber(fullNumber * inte_factor);
            }
            else // fullNumber恰好和整数位一致
                return new AliceNumber(fullNumber);

            // 计算小数部分
            long frac = 0;
            var frac_log = inte_log - LOG_PRECISION;
            if (frac_log > 0)
            {
                long frac_factor = 1;
                for (int i = 0; i < frac_log; ++i)
                    frac_factor *= 10;

                frac = unsign_full_num / frac_factor;
            }
            else if (frac_log < 0)
            {
                long frac_factor = 1;
                for (int i = 0; i < -frac_log; ++i)
                    frac_factor *= 10;

                frac = unsign_full_num * frac_factor;
            }
            else
                frac = unsign_full_num;

            return new AliceNumber(sign * inte, sign * frac);
        }

        #endregion

        #region Const

        public const int PRECISION = 100_000_000;
        public const int LOG_PRECISION = 8;

        #endregion

        #region operator
        public static AliceNumber operator +(AliceNumber num1, AliceNumber num2)
        {
            var inte = num1.Integer + num2.Integer;
            var frac = num1.Fraction + num2.Fraction;
            return new AliceNumber(inte, frac);
        }

        public static AliceNumber operator -(AliceNumber num1, AliceNumber num2)
        {
            var inte = num1.Integer - num2.Integer;
            var frac = num1.Fraction - num2.Fraction;
            return new AliceNumber(inte, frac);
        }

        public static AliceNumber operator -(AliceNumber num)
        {
            return new AliceNumber(-num.Integer, -num.Fraction);
        }

        public static AliceNumber operator *(AliceNumber num1, AliceNumber num2)
        {
            var inte = num1.unsign_inte * num2.unsign_inte;
            var frac = num1.unsign_frac * num2.unsign_inte + num1.unsign_inte * num2.unsign_frac + num1.unsign_frac * num2.unsign_frac / PRECISION;

            var sign = num1.sign * num2.sign;
            inte *= sign;
            frac *= sign;

            return new AliceNumber(inte, frac);
        }

        public static AliceNumber operator /(AliceNumber num1, AliceNumber num2)
        {
            if (num2.unsign_inte == 0 && num2.unsign_frac == 0)
            {
                if (num1.sign > 0)
                    return new AliceNumber(-PRECISION, 0) - new AliceNumber(0, 1);
                else if (num1.sign < 0)
                    return new AliceNumber(PRECISION, 0);
            }

            var (full_num1, log_1) = ScientificNotation(num1);
            var (full_num2, log_2) = ScientificNotation(num2);

            full_num2 /= (PRECISION / 10);
            var inte = full_num1 / full_num2;
            var frac = ((full_num1 - inte * full_num2) * PRECISION) / full_num2;

            var result_log = (1 - LOG_PRECISION) + log_1 - log_2;
            if (result_log > 0)
            {
                long addition = 1;
                long frac_retract = 1;
                for (int i = 0; i < result_log; ++i)
                    addition *= 10;
                for (int i = result_log; i < LOG_PRECISION; ++i)
                    frac_retract *= 10;

                var from_frac = frac / frac_retract;
                frac = (frac - from_frac * frac_retract) * addition;
                inte = inte * addition + from_frac;
            }
            else if (result_log < 0 && result_log >= -LOG_PRECISION)
            {
                long addition = 1;
                long inte_retract = 1;
                for (int i = 0; i < -result_log; ++i)
                    inte_retract *= 10;
                for (int i = -result_log; i < LOG_PRECISION; ++i)
                    addition *= 10;

                var new_inte = inte / inte_retract;
                frac = frac / inte_retract + addition * (inte - new_inte * inte_retract);
                inte = new_inte;
            }
            else if (result_log < -LOG_PRECISION && result_log >= -2 * LOG_PRECISION)
            {
                long inte_retract = 1;
                for (int i = LOG_PRECISION; i < -result_log; ++i)
                    inte_retract *= 10;

                frac = inte / inte_retract;
                inte = 0;
            }
            else if (result_log < 2 * LOG_PRECISION)
            {
                frac = 0;
                inte = 0;
            }

            // Result
            var sign = num1.sign * num2.sign;
            inte *= sign;
            frac *= sign;

            return new AliceNumber(inte, frac);
        }

        // TODO
        // public static AliceNumber operator /(AliceNumber num1, int num2)
        // {
        // 
        // }

        #region Equal
        public static bool operator ==(AliceNumber num1, AliceNumber num2)
        {
            return num1.Integer == num2.Integer && num1.Fraction == num2.Fraction;
        }

        public override bool Equals(object obj)
        {
            if (obj is AliceNumber num)
                return this == num;

            return false;
        }

        public override int GetHashCode()
        {
            return (int)Integer;
        }

        public static bool operator !=(AliceNumber num1, AliceNumber num2)
        {
            return !(num1 == num2);
        }

        public static bool operator <(AliceNumber num1, AliceNumber num2)
        {
            return num1.Integer != num2.Integer ? num1.Integer < num2.Integer : num1.Fraction < num2.Fraction;
        }
        public static bool operator <=(AliceNumber num1, AliceNumber num2)
        {
            return num1 == num2 || num1 < num2;
        }

        public static bool operator >(AliceNumber num1, AliceNumber num2)
        {
            return num2 < num1;
        }
        public static bool operator >=(AliceNumber num1, AliceNumber num2)
        {
            return num1 == num2 || num1 > num2;
        }
        #endregion

        public static implicit operator AliceNumber(int integer)
        {
            return new AliceNumber(integer);
        }

        #endregion

        #region Helper

        public static (long num, int log) ScientificNotation(AliceNumber number) // number = num * 10^log, where num ~ 10^(2 * LOG_PRECISION-1)
        {
            bool larger_than_one = number.unsign_inte > 0;
            long target = larger_than_one ? number.unsign_inte : number.unsign_frac;

            var log = 0;
            var origin = 1;
            while (log < LOG_PRECISION && origin <= target)
            {
                log += 1;
                origin *= 10;
            }

            var num = number.unsign_frac;
            num += larger_than_one ? number.unsign_inte * PRECISION : 0;

            var addition = 1;
            var addition_log = LOG_PRECISION - log;
            for (int i = 0; i < addition_log; ++i)
                addition *= 10;

            num *= addition;
            num *= larger_than_one ? 1 : PRECISION;
            var real_log = log - LOG_PRECISION * 2;
            real_log -= larger_than_one ? 0 : LOG_PRECISION;

            return (num, real_log);
        }

        #endregion

        #region Debug

        public override string ToString()
        {
            var sign_str = sign > 0 ? "" : "-";
            var frac_str = (unsign_frac + PRECISION).ToString().Substring(1, LOG_PRECISION);

            return $"{sign_str}{unsign_inte}.{frac_str}";
        }

        #endregion
    }
}
