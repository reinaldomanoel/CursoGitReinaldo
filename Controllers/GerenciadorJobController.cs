using CAST.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace CAST.Controllers
{
    public class GerenciadorJobController : Controller
    {

        private IRevisaoAppService _revisaoAppService;
        private ITrocaOrgaoAppService _trocaOrgaoAppService;
        private IRevogacaoDesligamentoAppService _revogacaoDesligamentoAppService;
        private IPedidoAppService _pedidoAppService;

        public GerenciadorJobController(IRevisaoAppService revisaoAppService, 
                                        ITrocaOrgaoAppService trocaOrgaoAppService,
                                        IRevogacaoDesligamentoAppService revogacaoDesligamentoAppService,
                                        IPedidoAppService pedidoAppService)
        {
            _revisaoAppService               = revisaoAppService;
            _trocaOrgaoAppService            = trocaOrgaoAppService;
            _revogacaoDesligamentoAppService = revogacaoDesligamentoAppService;
            _pedidoAppService                = pedidoAppService;
        }

        //
        // GET: /GerenciadorJob/
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult ExecutarJob(int job) 
        {
            try
            {
                string json = string.Empty;

                switch (job)
                {
                    case 1:
                        _revisaoAppService.RevalidaAcessosRevisoesProgramadas();
                        break;
                    case 2:
                        _revisaoAppService.RevalidaAcessosRevisoesExpiradas();
                        break;
                    case 3:
                        _trocaOrgaoAppService.ExecutarMudancaLotacao();
                        break;
                    case 4:
                        _trocaOrgaoAppService.RotinaRevalidacaoTrocaLotacaoExpirada();
                        break;
                    case 5:
                        _revogacaoDesligamentoAppService.RotinaRevogacaoDesligamento();
                        break;
                    case 6:
                        return Json(new { Status = HttpStatusCode.OK, Dados = "Não implementado!" }, JsonRequestBehavior.AllowGet);
                        
                    case 7:
                        _pedidoAppService.VerificarPedidoConcessaoRemocaoExpirado();
                        break;                   
                }

                return Json(new { Status = HttpStatusCode.OK, Dados = "Job executado com sucesso!!!" }, JsonRequestBehavior.AllowGet);
            }
            
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return Json(new { Status = HttpStatusCode.InternalServerError, Codigo = 0, Mensagem = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        
        }
	}
}