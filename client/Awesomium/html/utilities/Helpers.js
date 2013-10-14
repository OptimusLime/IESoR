function namespace(namespaceString) {
    var parts = namespaceString.split('.'),
        parent = window,
        currentPart = '';

    for(var i = 0, length = parts.length; i < length; i++) {
        currentPart = parts[i];
        parent[currentPart] = parent[currentPart] || {};
        parent = parent[currentPart];
    }

    return parent;
};

var componentToHex = function(c) {
    var hex = c.toString(16);
    return hex.length == 1 ? "0" + hex : hex;
}

var rgbToHex = function (r, g, b) {
    return "#" + componentToHex(r) + componentToHex(g) + componentToHex(b);
}

var stringToFunction = function(str) {
    var arr = str.split(".");

    var fn = (window || this);
    for (var i = 0, len = arr.length; i < len; i++) {
        fn = fn[arr[i]];
    }

    if (typeof fn !== "function") {
        throw new Error("function not found");
    }

    return  fn;
};

Math.sign = function(input)
{
    if (input < 0) {return -1};
    if (input > 0) {return 1};
    return 0;
};

//a different type of inheritance -- might be easier to read and convert C# code using this model
Function.prototype.inheritsFrom = function( parentClassOrObject ){
    if ( parentClassOrObject.constructor == Function )
    {
        //Normal Inheritance
        this.prototype = new parentClassOrObject;
        this.prototype.constructor = this;
        this.prototype.parent = parentClassOrObject.prototype;
    }
    else
    {
        //Pure Virtual Inheritance
        this.prototype = parentClassOrObject;
        this.prototype.constructor = this;
        this.prototype.parent = parentClassOrObject;
    }
    return this;
};


/* Example usage of functional inheritance
 * LivingThing = {
 beBorn : function(){
 this.alive = true;
 }
 }
 //
 //
 function Mammal(name){
 this.name=name;
 this.offspring=[];
 }
 Mammal.inheritsFrom( LivingThing );
 Mammal.prototype.haveABaby=function(){
 this.parent.beBorn.call(this);
 var newBaby = new this.constructor( "Baby " + this.name );
 this.offspring.push(newBaby);
 return newBaby;
 }
 //
 //
 function Cat( name ){
 this.name=name;
 }
 Cat.inheritsFrom( Mammal );
 Cat.prototype.haveABaby=function(){
 var theKitten = this.parent.haveABaby.call(this);
 alert("mew!");
 return theKitten;
 }
 Cat.prototype.toString=function(){
 return '[Cat "'+this.name+'"]';
 }
 *
 * */

//potential for adding some methods
// addMethod - By John Resig (MIT Licensed)
function addMethod(object, name, fn){
    var old = object[ name ];
    object[ name ] = function(){
        if ( fn.length == arguments.length )
            return fn.apply( this, arguments );
        else if ( typeof old == 'function' )
            return old.apply( this, arguments );
    };
}

// Array Remove - By John Resig (MIT Licensed)
Array.prototype.remove = function(from, to) {
    var rest = this.slice((to || from) + 1 || this.length);
    this.length = from < 0 ? this.length + from : from;
    return this.push.apply(this, rest);
};


Array.prototype.removeAll = function(filter) {
    for (var i = 0; i < this.length; i++) {
        if (filter(this[i])) {
            this.splice(i, 1);
            i--;
        }
    }
    return this;
};

