using IdGen;

namespace keynote_asp.Helpers
{
    public class SnowflakeGlobal
    {
        private static IdGenerator SnowflakeGen = new IdGenerator(0);

        public static long Generate()
        {
            return SnowflakeGen.CreateId();
        }


        public static string ToBase64(string snowflakeId)
        {
            return ToBase64(long.Parse(snowflakeId));
        }

        public static string ToBase64(long snowflakeId)
        {
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            var result = string.Empty;

            while (snowflakeId > 0)
            {
                result = chars[(int)(snowflakeId % 62)] + result;
                snowflakeId /= 62;
            }

            return result;
        }

        public static long FromBase64(string base64String)
        {
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            long result = 0;
            foreach (var c in base64String)
            {
                result = result * 62 + (long)chars.IndexOf(c);
            }

            return result;
        }

    }
}
