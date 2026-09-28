// Optional: regenerate the two card catalogs after deliberately editing the reviewed design.
const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '..');
const audit = JSON.parse(fs.readFileSync(path.join(root, 'docs/card-design-v0.2.json'), 'utf8'));
const definitions = audit.decks.slice(0, 21).flatMap((deck, arcana) => deck.cards.map((card, rank) => {
  const [cost, payment] = card.cost.split(' ');
  return { Arcana: arcana, Rank: rank, Name: card.name, Cost: +cost,
    Payment: { '混合': 'any', '费用': 'mana', '水晶': 'crystal' }[payment],
    Upright: card.upright, Reversed: card.reversed };
}));
const quoted = value => '@"' + value.replace(/"/g, '""') + '"';
const code = '// Generated from the reviewed v0.2 design. World remains outside this release.\nnamespace ArcanaDuel {\npublic static class CardCatalog {\n public static CardRule[] Create(){return new CardRule[]{\n' +
  definitions.map(card => '  new CardRule{' + Object.entries(card).map(([key, value]) => key + '=' +
    (typeof value === 'number' ? value : quoted(value))).join(',') + '}').join(',\n') + '\n };}\n}\n}\n';
fs.writeFileSync(path.join(root, 'src/CardCatalog.cs'), code);
fs.writeFileSync(path.join(root, 'cards.json'), JSON.stringify({ Version: '0.2', Definitions: definitions }, null, 2));
console.log('Generated ' + definitions.length + ' definitions. Synchronize Effects.cs, docs and tests when changing rules.');
