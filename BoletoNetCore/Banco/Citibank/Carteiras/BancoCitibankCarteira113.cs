using BoletoNetCore.Extensions;
using System;

namespace BoletoNetCore
{
    [CarteiraCodigo("1", "01", "001", "113")]
    public class BancoCitibankCarteira113 : ICarteira<BancoCitibank>
    {
        internal static Lazy<ICarteira<BancoCitibank>> Instance { get; } = new Lazy<ICarteira<BancoCitibank>>(() => new BancoCitibankCarteira113());

        private BancoCitibankCarteira113()
        {

        }

        public string FormataCodigoBarraCampoLivre(Boleto boleto)
        {
            if (boleto.CodigoConvenioBanco.Length > 20 || String.IsNullOrEmpty(boleto.CodigoConvenioBanco))
                throw new Exception($"Código da empresa inválido ({boleto.CodigoConvenioBanco})");

            var CodigoConvenioBanco = boleto.CodigoConvenioBanco.PadLeft(20, '0');
            var numeroOperacao = CodigoConvenioBanco.Substring(13, 7);

            // * Conta Cosmos
            // Número da conta cosmos da empresa junto ao Citibank. (Número da Conta Corrente)

            if (boleto.CodigoConvenioBanco.Length != 20 || string.IsNullOrEmpty(boleto.CodigoConvenioBanco))
                throw new Exception($"Código da empresa inválido ({boleto.CodigoConvenioBanco})");

            var portifolio = "113"; //CodigoConvenioBanco.Substring(17, 3);
            var two_cc = CodigoConvenioBanco.Substring(13, 3);
            var contaComos = $"{(CodigoConvenioBanco.Substring(0, 6) + two_cc)}";
            var nossoNumero = boleto.NossoNumero.Substring(0, 11).TrimStart(new Char[] { '0' }).PadLeft(11, '0');

            if (contaComos.Length > 9)
                contaComos.Substring(1, contaComos.Length - 1);

            return $"3{portifolio}{contaComos.PadLeft(9, '0')}{nossoNumero}{nossoNumero.CalcularDVCitibank()}";

        }

        public void FormataNossoNumero(Boleto boleto)
        {
            if (string.IsNullOrWhiteSpace(boleto.NossoNumero))
                throw new Exception("Nosso número não informado.");

            if (boleto.NossoNumero.Length > 11)
                throw new Exception($"Nosso Número ({boleto.NossoNumero}) deve conter 11 dígitos.");

            boleto.NossoNumero = boleto.NossoNumero.Substring(0, 11).PadLeft(11, '0');
            boleto.NossoNumeroDV = boleto.NossoNumero.CalcularDVCitibank();
            boleto.NossoNumeroFormatado = $"{boleto.NossoNumero}.{boleto.NossoNumeroDV}";
        }
    }
}
