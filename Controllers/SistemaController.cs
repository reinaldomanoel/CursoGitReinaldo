using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CAST.Application.Adapter;
using CAST.Application.Interfaces;
using CAST.Application.ViewModel;
using CAST.Business.Component;
using CAST.Filters;
using Newtonsoft.Json;
using PagedList;
using System.Net;
using System.Globalization;
using System.IO;
using log4net.Repository;
using log4net.Appender;
using log4net;
using CAST.Common.Helpers;

namespace CAST.Controllers
{
    public class SistemaController : BaseController
    {
        private readonly ISistemaAppService _sistemaAppService;

        public SistemaController(ISistemaAppService sistemaAppService)
        {
            _sistemaAppService = sistemaAppService;            
        }
        //
        // GET: /Sistema/
        public ActionResult Index()
        {
            return View(_sistemaAppService.ListarSistemasAlvo());
        }

        public ActionResult Ajuda()
        {
            return View();
        }

        [HttpPost]
        public JsonResult CadastrarSistemaAlvo(SistemaAlvoViewModel sistemaAlvoViewModel)
        {
            try
            {
                _sistemaAppService.Adicionar(sistemaAlvoViewModel, DadosUsuario().CodUsuario);
                return RetornoSucesso("Sistema cadastrado com sucesso!");
            }
            catch (BusinessException ex) 
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return RetornoMensagemErro(ex.Message);            
            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return Json(new { Status = HttpStatusCode.InternalServerError, Codigo = 0, Mensagem = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPut]
        public JsonResult AlterarSistemaAlvo(SistemaAlvoViewModel sistemaAlvoViewModel)
        {
            try
            {
                _sistemaAppService.Alterar(sistemaAlvoViewModel, DadosUsuario().CodUsuario);
                return RetornoSucesso("Sistema alterado com sucesso!");
            }
            catch (BusinessException ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return RetornoMensagemErro(ex.Message);
            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return Json(new { Status = HttpStatusCode.InternalServerError, Codigo = 0, Mensagem = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult BuscarSistemaAlvo(int codSistema)
        {
            try
            {
                return RetornoDados(_sistemaAppService.BuscarSistemaAlvo(codSistema));

            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return Json(new { Status = HttpStatusCode.InternalServerError, Codigo = 0, Mensagem = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpDelete]
        public JsonResult Excluir(int codSistema)
        {
            try
            {
                if (!_sistemaAppService.VeriricarPedidoRevisao(codSistema))
                {
                    _sistemaAppService.Excluir(codSistema);
                    return RetornoSucesso("Sistema excluído com sucesso!");
                }
                else
                {
                    return RetornoMensagemErro("Existe pedidos criados para esse sistema");
                }


            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return Json(new { Status = HttpStatusCode.InternalServerError, Codigo = 0, Mensagem = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        
        [HttpPut]
        public JsonResult AtivacaoSistema(int codSistema, bool ativacao)
        {
            try
            {
                _sistemaAppService.Ativacao(codSistema, ativacao, DadosUsuario().CodUsuario);

                var desc = !ativacao ? "ativado" : "desativado";

                return RetornoSucesso("Sistema " + desc + " com sucesso!");
            }
            catch (BusinessException ex) 
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return RetornoMensagemErro(ex.Message);

            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return Json(new { Status = HttpStatusCode.InternalServerError, Codigo = 0, Mensagem = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult ConsultaLog()
        {
            return View();
        }

        [HttpPost]
        public JsonResult PesquisarLog(string sistema)
        {
            List<LogSistemaViewModel> lLogs = new List<LogSistemaViewModel>();
         
            string lCaminhoLogs;

            DateTime lInicioPeriodo  = DateTime.Now.Date.AddDays(-29);
            DateTime lTerminoPeriodo = DateTime.Now.Date;

            lCaminhoLogs = RetornaCaminhoLogs();

            if (sistema.ToUpper().Contains("CA"))
            {
                lLogs.Add(new LogSistemaViewModel
                          {
                            nomeArquivo = "CA.log",
                            sistema     = sistema,
                            dataLog     = (new FileInfo(Path.Combine(lCaminhoLogs,"CA.log"))).CreationTime.Date
                          }
                         );

                
            }

            string[] arquivos = Directory.GetFiles(lCaminhoLogs, "*.log");

            foreach (string arquivo in arquivos.Where(p => p.ToLower().Contains(sistema.ToLower())))
            {
                LogSistemaViewModel log = new LogSistemaViewModel();
                string sDataLog;
                DateTime dataLog;
                sDataLog = AfterChar(Path.GetFileNameWithoutExtension(arquivo), "_");
                if (DateTime.TryParseExact(sDataLog, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dataLog))
                {
                    if (VerificaDataLog(log, lInicioPeriodo, lTerminoPeriodo, dataLog))
                    {
                        if (string.Equals(BeforeChar(Path.GetFileName(arquivo), "_"), (sistema), StringComparison.CurrentCultureIgnoreCase))
                        {
                            log.nomeArquivo = Path.GetFileName(arquivo);
                            log.sistema = sistema;
                            lLogs.Add(log);
                        }
                    }
                }                
            }

            return Json(lLogs.OrderByDescending(p => p.dataLog).ToList(), JsonRequestBehavior.AllowGet);
        }

        private string RetornaCaminhoLogs()
        {
            ILoggerRepository repo = LogManager.GetRepository();
            IAppender appender = repo.GetAppenders().FirstOrDefault();
            FileAppender fa = appender as FileAppender;

            return Path.GetDirectoryName(fa.File);
        }

        private Boolean VerificaDataLog(LogSistemaViewModel log, DateTime? lInicioPeriodo, DateTime? lTerminoPeriodo, DateTime dataLog)
        {
            if (lInicioPeriodo != null && lTerminoPeriodo != null)
            {
                if (dataLog >= lInicioPeriodo && dataLog <= lTerminoPeriodo)
                {
                    log.dataLog = dataLog;
                    return true;
                }
            }
            else
                if (lInicioPeriodo != null && lTerminoPeriodo == null)
            {
                if (dataLog >= lInicioPeriodo)
                {
                    log.dataLog = dataLog;
                    return true;
                }
            }
            else
                    if (lInicioPeriodo == null && lTerminoPeriodo != null)
            {
                if (dataLog <= lTerminoPeriodo)
                {
                    log.dataLog = dataLog;
                    return true;
                }
            }
            else
            {
                log.dataLog = dataLog;
                return true;
            }

            return false;
        }

        public string BeforeChar(string value, string a)
        {
            int posA = value.IndexOf(a);
            if (posA == -1)
            {
                return "";
            }
            return value.Substring(0, posA);
        }

        public string AfterChar(string value, string a)
        {
            int posA = value.LastIndexOf(a);
            if (posA == -1)
            {
                return "";
            }
            int adjustedPosA = posA + a.Length;
            if (adjustedPosA >= value.Length)
            {
                return "";
            }
            return value.Substring(adjustedPosA);
        }

       
        public JsonResult VerificarArquivo(string arquivo)
        {
            string sCaminhoDoArquivo = RetornaCaminhoLogs();

            bool arquivoExiste = (!Directory.Exists(Path.Combine(sCaminhoDoArquivo, arquivo)));

            return Json(arquivoExiste, JsonRequestBehavior.AllowGet);
            
        }

        public FileResult Download(string arquivo)
        {            
            string sCaminhoDoArquivo = RetornaCaminhoLogs();

            if (arquivo.ToUpper().Contains("CA.LOG"))
            {
                MemoryStream ms = new MemoryStream();
                using (FileStream file = new FileStream(Path.Combine(sCaminhoDoArquivo, arquivo), FileMode.Open, FileAccess.Read))
                    file.CopyTo(ms);


            }
            return File(System.IO.File.ReadAllBytes((Path.Combine(sCaminhoDoArquivo, arquivo))), "APPLICATION/OCTET-STREAM", arquivo);                
        }

        
        public JsonResult GeraArquivoRelatorioChavesAtivas()
        {
            try
            {
                
                var chavesAtivas = _sistemaAppService.ListarChavesAtivas();

                string[] cabecalho = new[] { "Chave", "Identificação", "Nome", "Lotação" };

                PlanilhaHelper excel = new PlanilhaHelper();

                excel.AdicionarPlanilha("Dados das Chaves Ativas");

                excel.GerarPlanilha(cabecalho, chavesAtivas);             

                string handle = Guid.NewGuid().ToString();

                TempData[handle] = excel.RetornarPlanilha();

                CultureInfo cult = new CultureInfo("pt-BR");
                string nomeArquivo = "RelatorioChavesAtivas_" + DateTime.Now.ToString("dd/MM/yyyy_HHmmss", cult) + ".xls";

                return Json(new { FileGuid = handle, FileName = nomeArquivo }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                throw;
            }
        }

        public virtual ActionResult DownloadRelatorioChavesAtivas(string fileGuid, string fileName)
        {
            if (TempData[fileGuid] != null)
            {
                byte[] data = TempData[fileGuid] as byte[];
                return File(data, "application/vnd.ms-excel", fileName);
            }
            else
            {
                return new EmptyResult();
            }
        }
    }
}