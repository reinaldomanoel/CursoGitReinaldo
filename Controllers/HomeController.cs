using CAST.Application.Adapter;
using CAST.Application.Interfaces;
using CAST.Application.ViewModel;
using CAST.Business.Component.Security;
using CAST.Filters;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Web.Mvc;

namespace CAST.Controllers
{
    [CustomAuthorize("Administrador de Sistema", "Gestor", "Administrador Geral", "Padrao")]
    public class HomeController : Controller
    {
        private readonly IPedidoAppService _pedidoAppService;

        public HomeController(IPedidoAppService pedidoAppService)
        {
            _pedidoAppService = pedidoAppService;
        }

        public ActionResult Index()
        {
            UsuarioSistema usuarioSistema = JsonConvert.DeserializeObject<UsuarioSistema>(Request.Cookies["UsuarioCAST"].Value);

            ViewBag.ListaMeusPedidos = _pedidoAppService.Listar(PedidoFiltroAdapter.ToFiltroDashBoardUsuarioPadraoSql(usuarioSistema), PedidoFiltroAdapter.ToOrdeBySql());

            var listaPedidosAvaliar = new List<PedidoViewModel>();

            if (usuarioSistema.Perfis.Exists(p => p.codigoPerfil == TipoPerfil.AdministradorSistema || p.codigoPerfil == TipoPerfil.Gestor))
            {
                listaPedidosAvaliar = _pedidoAppService.Listar(PedidoFiltroAdapter.ToFiltroDashBoardUsuarioAvaliadorSql(usuarioSistema), PedidoFiltroAdapter.ToOrdeBySql());
            }

            ViewBag.ListaPedidosParaAvaliar = listaPedidosAvaliar;

            ViewBag.Cookie = usuarioSistema;

            return View();
        }

        public ActionResult Ajuda()
        {
            return View();
        }


    }
}
