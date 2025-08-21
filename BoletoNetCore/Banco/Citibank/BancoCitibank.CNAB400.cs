using System;
using static System.String;

namespace BoletoNetCore
{
    partial class BancoCitibank : IBancoCNAB400
    {
        public string GerarDetalheRemessaCNAB400(Boleto boleto, ref int numeroRegistro)
        {
            string detalhe = GerarDetalheRemessaCNAB400Registro1(boleto, ref numeroRegistro);
            string strline = GerarDetalheRemessaCNAB400Registro2(boleto, ref numeroRegistro);
            if (!IsNullOrWhiteSpace(strline))
            {
                detalhe += Environment.NewLine;
                detalhe += strline;
            }
            return detalhe;
        }

        public void LerDetalheRetornoCNAB400Segmento1(ref Boleto boleto, string registro)
        {
            try
            {
                //Nº Controle do Participante
                boleto.NumeroControleParticipante = registro.Substring(37, 25);

                //Carteira (no arquivo retorno, vem com 1 caracter. Ajustamos para 2 caracteres, como no manual do Bradesco.
                boleto.Carteira = registro.Substring(107, 1).PadLeft(2, '0');
                boleto.TipoCarteira = TipoCarteira.CarteiraCobrancaSimples;

                //Identificação do Título no Banco
                boleto.NossoNumero = registro.Substring(64, 11); //Sem o DV
                boleto.NossoNumeroDV = registro.Substring(75, 1); //DV
                boleto.NossoNumeroFormatado = $"{boleto.Carteira}/{boleto.NossoNumero}-{boleto.NossoNumeroDV}";

                //Identificação de Ocorrência
                boleto.CodigoMovimentoRetorno = registro.Substring(108, 2);
                boleto.DescricaoMovimentoRetorno = DescricaoOcorrenciaCnab400(boleto.CodigoMovimentoRetorno);
                boleto.CodigoMotivoOcorrencia = registro.Substring(301, 20);

                //Número do Documento
                boleto.NumeroDocumento = registro.Substring(116, 10);
                boleto.EspecieDocumento = AjustaEspecieCnab400(registro.Substring(62, 2));

                //Valores do Título
                boleto.ValorTitulo = Convert.ToDecimal(registro.Substring(152, 13)) / 100;
                boleto.ValorTarifas = Convert.ToDecimal(registro.Substring(175, 13)) / 100;
                boleto.ValorOutrasDespesas = Convert.ToDecimal(registro.Substring(188, 13)) / 100;
                boleto.ValorIOF = Convert.ToDecimal(registro.Substring(214, 13)) / 100;
                boleto.ValorAbatimento = Convert.ToDecimal(registro.Substring(227, 13)) / 100;
                boleto.ValorDesconto = Convert.ToDecimal(registro.Substring(240, 13)) / 100;
                boleto.ValorPago = Convert.ToDecimal(registro.Substring(253, 13)) / 100;
                boleto.ValorJurosDia = Convert.ToDecimal(registro.Substring(266, 13)) / 100;                

                //Data Ocorrência no Banco
                boleto.DataProcessamento = Utils.ToDateTime(Utils.ToInt32(registro.Substring(110, 6)).ToString("##-##-##"));

                //Data Vencimento do Título
                boleto.DataVencimento = Utils.ToDateTime(Utils.ToInt32(registro.Substring(146, 6)).ToString("##-##-##"));

                // Data do Crédito
                boleto.DataCredito = Utils.ToDateTime(Utils.ToInt32(registro.Substring(295, 6)).ToString("##-##-##"));

                // Registro Retorno
                boleto.RegistroArquivoRetorno = boleto.RegistroArquivoRetorno + registro + Environment.NewLine;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao ler detalhe do arquivo de RETORNO / CNAB 400.", ex);
            }
        }

        public void LerDetalheRetornoCNAB400Segmento7(ref Boleto boleto, string registro)
        {
            throw new NotImplementedException();
        }

        // *
        public string GerarHeaderRemessaCNAB400(ref int numeroArquivoRemessa, ref int numeroRegistroGeral)
        {
            try
            {
                numeroRegistroGeral++;
                var reg = new TRegistroEDI();
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0001, 001, 0, "0", '0');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0002, 001, 0, "1", '0');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0003, 007, 0, "REMESSA", ' ');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0010, 002, 0, "01", '0');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0012, 015, 0, "COBRANCA", ' ');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0027, 020, 0, Beneficiario.Codigo, '0');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0047, 030, 0, Beneficiario.Nome, ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0077, 018, 0, "745CITIBANK", ' ');
                reg.Adicionar(TTiposDadoEDI.ediDataDDMMAA___________, 0095, 006, 0, DateTime.Now, ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0101, 005, 0, "01600", ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0106, 003, 0, "BPI", ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0109, 006, 0, Empty, ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0115, 280, 0, Empty, ' ');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0395, 006, 0, numeroRegistroGeral, '0');
                reg.CodificarLinha();
                return reg.LinhaRegistro;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao gerar HEADER do arquivo de remessa do CNAB400.", ex);
            }
        }

        private string GerarDetalheRemessaCNAB400Registro1(Boleto boleto, ref int numeroRegistroGeral)
        {
            try
            {
                numeroRegistroGeral++;
                var reg = new TRegistroEDI();

                // 001/001 - Identificação do registro detalhe (fixo "1")
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0001, 001, 0, "1", '0');

                // 002/003 - Tipo de inscrição da empresa (01=CPF, 02=CNPJ)
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0002, 002, 0, boleto.Banco.Beneficiario.TipoCPFCNPJ("0"), '0');

                // 004/017 - Número de inscrição da empresa (CPF ou CNPJ)
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0004, 014, 0, boleto.Banco.Beneficiario.CPFCNPJ, ' ');

                // 018/037 - Identificação da empresa no Citibank
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0018, 020, 0, boleto.Banco.Beneficiario.Codigo, '0');

                // 038/062 - Identificação do título na empresa (livre)
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0038, 025, 0, boleto.NumeroControleParticipante, '0');

                // 063/064 - Espécie do título
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0063, 002, 0, AjustaEspecieCnab400(boleto.EspecieDocumento), ' ');

                // 065/076 - Nosso Número
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0065, 011, 0, boleto.NossoNumero, '0');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0076, 001, 0, boleto.NossoNumeroDV, '0');

                // 077/081 - Filler (brancos)
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0077, 005, 0, Empty, ' ');

                // 082/082 - Identificação da emissão do boleto
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0082, 001, 0, "0", '0');

                // 083/088 - Data do desconto
                reg.Adicionar(TTiposDadoEDI.ediDataDDMMAA___________, 0083, 006, 0, boleto.DataDesconto2, '0');

                // 089/101 - Valor do desconto
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0089, 013, 2, boleto.ValorDesconto2, '0');

                // 102/107 - Filler (brancos)
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0102, 006, 0, Empty, ' ');

                // 108/108 - Código da Carteira
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0108, 001, 0, boleto.Carteira, '0');

                // 109/110 - Código da Ocorrência
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0109, 002, 0, boleto.CodigoMotivoOcorrencia, '0');

                // 111/120 - Seu número (referência do cliente)
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0111, 010, 0, boleto.NumeroDocumento, '0');

                // 121/126 - Data do Vencimento do Título
                reg.Adicionar(TTiposDadoEDI.ediDataDDMMAA___________, 0121, 006, 0, boleto.DataVencimento, '0');

                // 127/139 - Valor do Título
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0127, 013, 2, boleto.ValorTitulo, '0');

                // 140/142 - Número do Citibank na Compensação
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0140, 003, 0, "745", '0');

                // 143/147 - Brancos – Uso exclusivo do Banco
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0143, 005, 0, "0", '0');

                // 148/149 - Tipo de Emissão (07 - Banco não imprime)
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0148, 002, 0, "07", ' ');

                // 150/150 - Aceite do Título
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0150, 001, 0, boleto.Aceite, ' ');

                // 151/156 - Data da Emissão do Título
                reg.Adicionar(TTiposDadoEDI.ediDataDDMMAA___________, 0151, 006, 0, boleto.DataEmissao, '0');

                // 157/159 - Código da Instrução
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0157, 002, 0, boleto.CodigoInstrucao1, ' ');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0159, 002, 0, boleto.CodigoInstrucao2, ' ');

                // 160/172 - Valor da Mora por Dia de Atraso
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0161, 013, 2, boleto.ValorJurosDia, '0');
                if (boleto.ValorDesconto == 0)
                    reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0174, 006, 0, "0", '0'); // Sem Desconto
                else
                    reg.Adicionar(TTiposDadoEDI.ediDataDDMMAA___________, 0174, 006, 0, boleto.DataDesconto, '0'); // Com Desconto

                // 180/192 - Valor do Desconto
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0180, 013, 2, boleto.ValorDesconto, '0');

                // 193/193 - Filler (brancos)
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0193, 004, 0, Empty, ' ');

                // 197/205 - Valor de Outras Despesas
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0197, 009, 2, boleto.ValorIOF, '0');

                // 206/218 - Valor do Abatimento
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0206, 013, 2, boleto.ValorAbatimento, '0');

                // 219/394 - Dados do Pagador
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0219, 002, 0, boleto.Pagador.TipoCPFCNPJ("00"), '0');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0221, 014, 0, boleto.Pagador.CPFCNPJ, '0');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0235, 040, 0, boleto.Pagador.Nome, ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0275, 040, 0, boleto.Pagador.Endereco.FormataLogradouro(40), ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0315, 012, 0, boleto.Pagador.Endereco.Bairro, ' ');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0327, 008, 0, boleto.Pagador.Endereco.CEP.Replace("-", ""), '0');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0335, 015, 0, boleto.Pagador.Endereco.Cidade, ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0350, 002, 0, boleto.Pagador.Endereco.UF, ' ');

                // 351/392 - Mensagem 1
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0352, 040, 0, boleto.MensagemArquivoRemessa, ' ');

                // 393/394 - Filler (brancos)
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0392, 002, 0, Empty, ' ');

                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0395, 006, 0, boleto.CodigoMoeda, ' ');

                // 395/400 - Número sequencial do registro
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0395, 006, 0, numeroRegistroGeral, '0');

                reg.CodificarLinha();
                return reg.LinhaRegistro;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao gerar DETALHE do arquivo CNAB400 - Registro 1.", ex);
            }
        }

        // *
        private string GerarDetalheRemessaCNAB400Registro2(Boleto boleto, ref int numeroRegistroGeral)
        {
            try
            {
                if (IsNullOrWhiteSpace(boleto.MensagemArquivoRemessa))
                    return "";

                numeroRegistroGeral++;
                var reg = new TRegistroEDI();

                // 001/001 - Identificação do registro detalhe (fixo "3")
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0001, 001, 0, "3", '0');

                // 002/003 - Tipo de inscrição da empresa (01=CPF, 02=CNPJ)
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0002, 002, 0, boleto.Banco.Beneficiario.TipoCPFCNPJ("0"), '0');

                // 004/017 - Número de inscrição da empresa (CPF ou CNPJ)
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0004, 014, 0, boleto.Banco.Beneficiario.CPFCNPJ, '0');

                // 018/037 - Identificação da empresa no Citibank
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0018, 020, 0, boleto.Banco.Beneficiario.Codigo, ' ');

                // 038/062 - Identificação do título na empresa (livre)
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0038, 025, 0, boleto.NumeroControleParticipante, ' ');

                // 063/064 - Espécie do título
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0063, 002, 0, AjustaEspecieCnab400(boleto.EspecieDocumento), '0');

                // 065/076 - Nosso Número
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0065, 012, 0, boleto.NossoNumero, '0');

                // 077/077 - Código da Carteira (1=Simples, 2=Caucionada)
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0077, 001, 0, boleto.Carteira, '0');

                // 078/087 - Seu número (referência do cliente)
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0078, 010, 0, boleto.NumeroDocumento, ' ');

                // 088/091 - Filler (brancos)
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0088, 004, 0, string.Empty, ' ');

                // 092/093 - Ocorrência
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0092, 002, 0, boleto.CodigoMotivoOcorrencia, '0');

                // 094/106 - Mínimo para pagamento (9(9)v9(4))
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0094, 013, 4, "0", '0');

                // 107/119 - Máximo para pagamento (9(9)v9(4))
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0107, 013, 4, "0", '0');

                // 120/120 - Aceite pagamento divergente (Valor Total, sem diferenças)
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0120, 001, 0, "3", '0');

                // 121/121 - Código da multa
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0121, 001, 0, boleto.TipoJuros, ' ');

                // 122/129 - Data da multa (DDMMAAAA)
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0122, 008, 0, boleto.DataMulta, '0');

                // 130/142 - Valor ou percentual da multa
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0130, 013, 2, boleto.ValorMulta, '0');

                // 143/150 - Data do juros de mora
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0143, 008, 0, boleto.DataJuros, '0');

                // 151/151 - Periodicidade do juros (dia)
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0151, 001, 0, "1", ' ');

                // 152/316 - Uso exclusivo do banco (brancos)
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0152, 165, 0, string.Empty, ' ');

                // 317/317 - Apresentação de mínimo (1=sim, 2=não)
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0317, 001, 0, "2", ' ');

                // 318/394 - Brancos
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0318, 077, 0, string.Empty, ' ');

                // 395/400 - Número sequencial do registro
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0395, 006, 0, numeroRegistroGeral, '0');

                reg.CodificarLinha();
                return reg.LinhaRegistro;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao gerar DETALHE do arquivo CNAB400 - Registro 2.", ex);
            }
        }

        // *
        public string GerarTrailerRemessaCNAB400(int numeroRegistroGeral, decimal valorBoletoGeral, int numeroRegistroCobrancaSimples, decimal valorCobrancaSimples, int numeroRegistroCobrancaVinculada, decimal valorCobrancaVinculada, int numeroRegistroCobrancaCaucionada, decimal valorCobrancaCaucionada, int numeroRegistroCobrancaDescontada, decimal valorCobrancaDescontada)
        {
            try
            {
                numeroRegistroGeral++;
                var reg = new TRegistroEDI();
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0001, 001, 0, "9", '0');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0002, 393, 0, Empty, ' ');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0395, 006, 0, numeroRegistroGeral, '0');
                reg.CodificarLinha();
                return reg.LinhaRegistro;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro durante a geração do registro TRAILER do arquivo de REMESSA.", ex);
            }
        }

        // *
        private string DescricaoOcorrenciaCnab400(string codigo)
        {
            switch (codigo)
            {
                case "01":
                    return "Remessa";

                case "02":
                    return "Pedido de Baixa/Devolução";

                case "04":
                    return "Concessão de Abatimento";

                case "06":
                    return "Prorrogação de Vencimento/Valor Nominal";

                case "07":
                    return "Concessão de Desconto";

                case "09":
                    return "Pedido de Protesto";

                case "10":
                    return "Sustar protesto";

                case "12":
                    return "Alteração de Juros de Mora";

                case "13":
                    return "Dispensar Cobrança de Juros de Mora";

                case "16":
                    return "Alteração de Valor/Data de Desconto";

                case "18":
                    return "Alteração do Abatimento";

                case "23":
                case "31":
                    return "Alteração de dados do Sacado";

                case "29":
                    return "Pedido de negativação";

                case "39":
                    return "Pedido de exclusão de negativação";

                case "49":
                    return "Sustar protesto e baixar";

                default:
                    return "";
            }
        }

        // *
        private TipoEspecieDocumento AjustaEspecieCnab400(string codigoEspecie)
        {
            switch (codigoEspecie)
            {
                case "00":
                    return TipoEspecieDocumento.DMI;
                case "01":
                    return TipoEspecieDocumento.DM;
                default:
                    return TipoEspecieDocumento.OU;
            }
        }

        // *
        private string AjustaEspecieCnab400(TipoEspecieDocumento especieDocumento)
        {
            switch (especieDocumento)
            {
                case TipoEspecieDocumento.DMI:
                    return "00";
                case TipoEspecieDocumento.DM:
                    return "01";
                default:
                    return "99";
            }
        }

        public void LerTrailerRetornoCNAB400(string registro)
        {
        }

        public override void CompletarHeaderRetornoCNAB400(string registro)
        {
            //021 a 037 - Identificações da Empresa Beneficiária no Banco
            //Deverá ser preenchido(esquerda para direita), da seguinte maneira:
            //21 a 21 - Zero
            //22 a 24 - códigos da carteira
            //25 a 29 - códigos da Agência Beneficiários, sem o dígito.
            //30 a 36 - Contas Corrente
            //37 a 37 - dígitos da Conta

            this.Beneficiario.ContaBancaria = new ContaBancaria();
            this.Beneficiario.ContaBancaria.Agencia = registro.Substring(24, 5);

            var conta = registro.Substring(29, 8).Trim();
            this.Beneficiario.ContaBancaria.Conta = conta.Substring(0, 7);
            this.Beneficiario.ContaBancaria.DigitoConta = conta.Substring(7, 1);

            // 01 - cpf / 02 - cnpj
            if (registro.Substring(1, 2) == "01")
                this.Beneficiario.CPFCNPJ = registro.Substring(6, 11);
            else
                this.Beneficiario.CPFCNPJ = registro.Substring(3, 14);
        }
    }
}
