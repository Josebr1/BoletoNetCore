using BoletoNetCore.Exceptions;
using System.Collections.Generic;

namespace BoletoNetCore
{
    internal sealed partial class BancoCitibank : BancoFebraban<BancoCitibank>, IBanco
    {

        public BancoCitibank()
        {
            Codigo = 745;
            Nome = "CITIBANK";
            Digito = "5";
            IdsRetornoCnab400RegistroDetalhe = new List<string> { "1" };
            RemoveAcentosArquivoRemessa = true;
        }

        public void FormataBeneficiario()
        {
            var contaBancaria = Beneficiario.ContaBancaria;

            if (!CarteiraFactory<BancoCitibank>.CarteiraEstaImplementada(contaBancaria.CarteiraComVariacaoPadrao))
                throw BoletoNetCoreException.CarteiraNaoImplementada(contaBancaria.CarteiraComVariacaoPadrao);

            contaBancaria.FormatarDados("PAGÁVEL NA REDE BANCÁRIA ATÉ O VENCIMENTO", "", "", 12);

            Beneficiario.CodigoFormatado = $"{contaBancaria.Agencia} {contaBancaria.Conta}{contaBancaria.DigitoConta}";
        }

        public string GerarMensagemRemessa(TipoArquivo tipoArquivo, Boleto boleto, ref int numeroRegistro)
        {
            return null;
        }
    }
}
