using CAST.Application.Adapter;
us<PedidoViewModel>();

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
