using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Bookify.Web.Services
{
    /// <summary>
    /// VNPay Payment Service - theo đúng tài liệu chính thức VNPay 2.1.0
    ///
    /// HASH DATA RULE (từ PHP demo chính thức):
    ///   hashdata += urlencode(key) + "=" + urlencode(value) + "&"
    ///   → encode CẢ key VÀ value
    /// </summary>
    public class VNPayService
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public VNPayService(IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _config = config;
            _httpContextAccessor = httpContextAccessor;
        }

        public string CreatePaymentUrl(int orderId, decimal amount, string orderInfo)
        {
            string tmnCode    = _config["VNPay:TmnCode"]!;
            string hashSecret = _config["VNPay:HashSecret"]!;
            string baseUrl    = _config["VNPay:BaseUrl"]!;
            string returnUrl  = _config["VNPay:ReturnUrl"]!;

            // IP
            var ctx = _httpContextAccessor.HttpContext!;
            string ipAddr = ctx.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
            if (string.IsNullOrEmpty(ipAddr) || ipAddr == "0.0.0.1") ipAddr = "127.0.0.1";

            string createDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            string txnRef     = orderId.ToString();
            long   vnpAmount  = (long)(amount * 100);

            // Sort theo key alphabet (StringComparer.Ordinal = byte order, giống ksort PHP)
            var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Amount"]     = vnpAmount.ToString(),
                ["vnp_Command"]    = "pay",
                ["vnp_CreateDate"] = createDate,
                ["vnp_CurrCode"]   = "VND",
                ["vnp_IpAddr"]     = ipAddr,
                ["vnp_Locale"]     = "vn",
                ["vnp_OrderInfo"]  = orderInfo,
                ["vnp_OrderType"]  = "other",
                ["vnp_ReturnUrl"]  = returnUrl,
                ["vnp_TmnCode"]    = tmnCode,
                ["vnp_TxnRef"]     = txnRef,
                ["vnp_Version"]    = "2.1.0",
            };

            // Theo PHP demo chính thức:
            //   hashdata += urlencode(key) + "=" + urlencode(value)
            //   query    += urlencode(key) + "=" + urlencode(value)
            var hashData  = new StringBuilder();
            var queryData = new StringBuilder();

            foreach (var kv in vnpParams)
            {
                if (string.IsNullOrEmpty(kv.Value)) continue;

                string encodedKey   = WebUtility.UrlEncode(kv.Key);
                string encodedValue = WebUtility.UrlEncode(kv.Value);

                if (hashData.Length > 0)  hashData.Append('&');
                hashData.Append(encodedKey).Append('=').Append(encodedValue);

                if (queryData.Length > 0) queryData.Append('&');
                queryData.Append(encodedKey).Append('=').Append(encodedValue);
            }

            string secureHash = HmacSHA512(hashSecret, hashData.ToString());
            string paymentUrl = $"{baseUrl}?{queryData}&vnp_SecureHash={secureHash}";
            return paymentUrl;
        }

        public bool ValidateSignature(IQueryCollection query)
        {
            string hashSecret    = _config["VNPay:HashSecret"]!;
            string vnpSecureHash = query["vnp_SecureHash"].ToString();

            var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var key in query.Keys)
            {
                if (key.StartsWith("vnp_")
                    && key != "vnp_SecureHash"
                    && key != "vnp_SecureHashType")
                {
                    vnpParams[key] = query[key].ToString();
                }
            }

            // Cùng cách build hash như lúc tạo URL
            var hashData = new StringBuilder();
            foreach (var kv in vnpParams)
            {
                if (string.IsNullOrEmpty(kv.Value)) continue;
                if (hashData.Length > 0) hashData.Append('&');
                hashData
                    .Append(WebUtility.UrlEncode(kv.Key))
                    .Append('=')
                    .Append(WebUtility.UrlEncode(kv.Value));
            }

            string checkHash = HmacSHA512(hashSecret, hashData.ToString());
            return checkHash.Equals(vnpSecureHash, StringComparison.OrdinalIgnoreCase);
        }

        private static string HmacSHA512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
