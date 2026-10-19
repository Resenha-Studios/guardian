# Changelog - Guardian

Todos os lançamentos notáveis deste pacote serão documentados neste arquivo.

## [0.1.0] - 2026-10-19

### Adicionado
- Setup inicial da estrutura UPM de `com.resenha.guardian`.
- Core Runner com pipeline modular de scanners e cálculo de Health Score ponderado.
- `MissingScriptsScanner`: Detecção e remoção com 1 clique (Quick Fix) de scripts ausentes em Prefabs e Cenas.
- `MissingReferencesScanner`: Detecção de campos serializados corrompidos/nulos.
- `CodeAntiPatternsScanner`: Detecção em código C# de chamadas de `FindObjectOfType`, `GameObject.Find`, `Camera.main`, `GetComponent` e alocações de coleções dentro de loops de frame (`Update`, `FixedUpdate`, `LateUpdate`, `OnGUI`).
- `LocalizationComplianceScanner`: Validação de textos estáticos hardcoded em arquivos UXML de UI Toolkit.
- `GuardianWindow`: Interface em UI Toolkit com atalho `Ctrl+G` / `Cmd+G`, seletor de idiomas (EN, PT, ES, JA, ZH), filtros de severidade e ações de Ping/Open Line.
- Botão `Copy Report`: Exportação e cópia instantânea para a área de transferência do relatório completo de auditoria formatado em Markdown, pronto para compartilhamento com IAs (Gemini/Claude) ou equipe de desenvolvimento.
